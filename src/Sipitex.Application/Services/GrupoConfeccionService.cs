using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class GrupoConfeccionService : IGrupoConfeccionService
{
    private readonly IGrupoConfeccionRepository _grupos;
    private readonly IProductionOrderRepository _orders;
    private readonly IUserRepository _users;
    private readonly IConsumoMaterialRepository _consumos;
    private readonly IUnitOfWork _unitOfWork;

    public GrupoConfeccionService(
        IGrupoConfeccionRepository grupos,
        IProductionOrderRepository orders,
        IUserRepository users,
        IConsumoMaterialRepository consumos,
        IUnitOfWork unitOfWork)
    {
        _grupos = grupos;
        _orders = orders;
        _users = users;
        _consumos = consumos;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<GrupoConfeccionDto>> GetByOrderAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _grupos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<GrupoConfeccionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _grupos.GetAllAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<ServiceResult> CreateAsync(CreateGrupoConfeccionDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.CantidadPrendas < 0)
            return ServiceResult.Fail("La cantidad de prendas no puede ser negativa.");

        var order = await _orders.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden de producción no encontrada.");

        var instructor = await _users.GetByIdAsync(dto.InstructorUserId, cancellationToken);
        if (instructor is null || !string.Equals(instructor.Rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
            return ServiceResult.Fail("El responsable debe ser un instructor activo.");

        if (dto.HoraFin is TimeOnly fin && fin < dto.HoraInicio)
            return ServiceResult.Fail("La hora de finalización no puede ser anterior a la de inicio.");

        await _grupos.AddAsync(new GrupoConfeccion
        {
            ProductionOrderId = order.Id,
            InstructorUserId = instructor.Id,
            FechaRealizacion = dto.FechaRealizacion == default
                ? DateOnly.FromDateTime(DateTime.Today)
                : dto.FechaRealizacion,
            HoraInicio = dto.HoraInicio,
            HoraFin = dto.HoraFin,
            CantidadPrendas = dto.CantidadPrendas
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Grupo de confección creado para {order.OrderNumber}.");
    }

    public async Task<ServiceResult> CerrarAsync(
        int grupoId,
        TimeOnly horaFin,
        int cantidadPrendas,
        CancellationToken cancellationToken = default)
    {
        var grupo = await _grupos.GetByIdAsync(grupoId, cancellationToken);
        if (grupo is null)
            return ServiceResult.Fail("Grupo de confección no encontrado.");
        if (horaFin < grupo.HoraInicio)
            return ServiceResult.Fail("La hora de finalización no puede ser anterior a la de inicio.");
        if (cantidadPrendas < 0)
            return ServiceResult.Fail("La cantidad de prendas no puede ser negativa.");

        grupo.HoraFin = horaFin;
        grupo.CantidadPrendas = cantidadPrendas;
        _grupos.Update(grupo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Grupo de confección cerrado.");
    }

    public async Task<GrupoConsumoCruzadoDto?> GetConsumosCruzadosAsync(
        int grupoId,
        CancellationToken cancellationToken = default)
    {
        var grupo = await _grupos.GetByIdAsync(grupoId, cancellationToken);
        if (grupo is null)
            return null;

        var consumos = await _consumos.GetByOrderIdAsync(grupo.ProductionOrderId, cancellationToken);
        var delGrupo = consumos.Where(c => c.GrupoConfeccionId is null || c.GrupoConfeccionId == grupo.Id).ToList();
        return new GrupoConsumoCruzadoDto(
            Map(grupo),
            grupo.ProductionOrder?.OrderNumber ?? string.Empty,
            delGrupo.Select(c => new ConsumoMaterialDto(
                c.Id,
                c.ProductionOrderId,
                c.ProductionOrder?.OrderNumber ?? string.Empty,
                c.GrupoConfeccionId,
                c.MaterialId,
                c.Material?.Code ?? string.Empty,
                c.Material?.Name ?? string.Empty,
                c.Cantidad,
                c.Material is null ? "—" : Helpers.UnitHelper.ToDisplay(c.Material.Unit),
                c.FechaUtc,
                c.ResponsableUserId,
                c.Responsable?.Nombre ?? $"#{c.ResponsableUserId}",
                c.CostoUnitario,
                c.Cantidad * c.CostoUnitario)).ToList());
    }

    private static GrupoConfeccionDto Map(GrupoConfeccion g) => new(
        g.Id,
        g.ProductionOrderId,
        g.ProductionOrder?.OrderNumber ?? string.Empty,
        g.InstructorUserId,
        g.Instructor?.Nombre ?? $"#{g.InstructorUserId}",
        g.FechaRealizacion,
        g.HoraInicio,
        g.HoraFin,
        g.CantidadPrendas);
}
