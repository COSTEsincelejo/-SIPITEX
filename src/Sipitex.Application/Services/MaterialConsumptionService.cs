using System.Globalization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// El consumo registra lo usado de lo ya entregado. No mueve stock: eso ocurre en la entrega.
public class MaterialConsumptionService : IMaterialConsumptionService
{
    private readonly IConsumoMaterialRepository _consumos;
    private readonly IProductionOrderRepository _orders;
    private readonly IMaterialRepository _materials;
    private readonly IUserRepository _users;
    private readonly IOrderMaterialRequirementRepository _requirements;
    private readonly IMaterialConsumptionCostService _costService;
    private readonly IUnitOfWork _unitOfWork;

    public MaterialConsumptionService(
        IConsumoMaterialRepository consumos,
        IProductionOrderRepository orders,
        IMaterialRepository materials,
        IUserRepository users,
        IOrderMaterialRequirementRepository requirements,
        IMaterialConsumptionCostService costService,
        IUnitOfWork unitOfWork)
    {
        _consumos = consumos;
        _orders = orders;
        _materials = materials;
        _users = users;
        _requirements = requirements;
        _costService = costService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ConsumoMaterialDto>> GetByOrderAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _consumos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<MaterialConsumibleOrdenDto>> GetMaterialesDeLaOrdenAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var lines = await _requirements.GetByOrderIdAsync(productionOrderId, cancellationToken);
        return lines.Select(l => new MaterialConsumibleOrdenDto(
            l.MaterialId,
            l.Material?.Name ?? $"#{l.MaterialId}",
            l.QuantityDelivered,
            l.QuantityConsumed,
            l.QuantityAvailableToConsume,
            UnitHelper.ToDisplay(l.Material?.Unit ?? l.Unit),
            l.Material?.CodigoAlmacen,
            l.Material?.Referencia,
            l.Material?.Categoria,
            l.Material?.TipoElemento ?? TipoElemento.Consumible)).ToList();
    }

    public async Task<ServiceResult> RegisterAsync(
        RegisterConsumoMaterialDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Cantidad <= 0)
            return ServiceResult.Fail("La cantidad utilizada debe ser mayor que cero.");
        if (dto.ResponsableUserId <= 0)
            return ServiceResult.Fail("Indique el responsable del consumo.");

        var order = await _orders.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden de producción no encontrada.");
        if (order.Status == OrderStatus.Finalizada)
            return ServiceResult.Fail("No se puede registrar consumo: la orden está finalizada.");
        if (order.Status == OrderStatus.Cancelada)
            return ServiceResult.Fail("No se puede registrar consumo: la orden está cancelada.");

        var material = await _materials.GetByIdAsync(dto.MaterialId, cancellationToken);
        if (material is null)
            return ServiceResult.Fail("Material no encontrado.");

        var responsable = await _users.GetByIdAsync(dto.ResponsableUserId, cancellationToken);
        if (responsable is null)
            return ServiceResult.Fail("Responsable no encontrado.");

        var lines = await _requirements.GetByOrderIdAsync(order.Id, cancellationToken);
        var line = lines.FirstOrDefault(l => l.MaterialId == material.Id);
        if (line is null)
            return ServiceResult.Fail($"«{material.Name}» no pertenece a la orden {order.OrderNumber}.");

        if (dto.Cantidad > line.QuantityAvailableToConsume)
            return ServiceResult.Fail(
                $"Solo se entregaron {Cantidad(line.QuantityDelivered)} de «{material.Name}»; ya consumiste {Cantidad(line.QuantityConsumed)}.");

        var costo = line.QuantityDelivered <= 0
            ? 0
            : decimal.Round(line.CostoUnitarioEntrega, 4, MidpointRounding.AwayFromZero);
        var fecha = dto.FechaUtc ?? DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            line.QuantityConsumed += dto.Cantidad;
            _requirements.Update(line);

            await _consumos.AddAsync(new ConsumoMaterial
            {
                ProductionOrderId = order.Id,
                GrupoConfeccionId = dto.GrupoConfeccionId,
                MaterialId = material.Id,
                Cantidad = dto.Cantidad,
                FechaUtc = fecha,
                ResponsableUserId = responsable.Id,
                CostoUnitarioAlMomento = costo
            }, ct);
        }, cancellationToken);

        return ServiceResult.Ok(
            $"Consumo de {Cantidad(dto.Cantidad)} {UnitHelper.ToDisplay(material.Unit)} de «{material.Name}» registrado en {order.OrderNumber}.");
    }

    public async Task<ConsumoCostoResumenDto> GetCostoPromedioByOrderAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _consumos.GetByOrderIdAsync(productionOrderId, cancellationToken);
        var lineas = rows.Select(r => new ConsumoCostoLineaDto(r.Cantidad, r.CostoUnitarioAlMomento)).ToList();
        var promedio = _costService.CalcularPromedioPonderado(lineas);
        var totalCantidad = lineas.Sum(l => l.Cantidad);
        var totalCosto = lineas.Sum(l => l.Cantidad * l.CostoUnitario);
        return new ConsumoCostoResumenDto(productionOrderId, totalCantidad, promedio, totalCosto);
    }

    private static string Cantidad(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static ConsumoMaterialDto Map(ConsumoMaterial c) => new(
        c.Id,
        c.ProductionOrderId,
        c.ProductionOrder?.OrderNumber ?? string.Empty,
        c.GrupoConfeccionId,
        c.MaterialId,
        c.Material?.Code ?? string.Empty,
        c.Material?.Name ?? string.Empty,
        c.Cantidad,
        c.Material is null ? "—" : UnitHelper.ToDisplay(c.Material.Unit),
        c.FechaUtc,
        c.ResponsableUserId,
        c.Responsable?.Nombre ?? $"#{c.ResponsableUserId}",
        c.CostoUnitarioAlMomento,
        c.Cantidad * c.CostoUnitarioAlMomento,
        c.Material?.CodigoAlmacen,
        c.Material?.Referencia,
        c.Material?.Categoria,
        c.Material?.TipoElemento);
}
