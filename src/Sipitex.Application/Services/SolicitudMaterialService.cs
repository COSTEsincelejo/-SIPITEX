using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Crear y consultar SolicitudMaterial (PorFicha / InsumosLibres)
public class SolicitudMaterialService : ISolicitudMaterialService
{
    private readonly ISolicitudMaterialRepository _solicitudRepository;
    private readonly IFichaRepository _fichaRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IBodegaRepository _bodegaRepository;
    private readonly ICodigoGeneradorService _codigoGenerador;
    private readonly IAlertService _alertService;
    private readonly IUnitOfWork _unitOfWork;

    public SolicitudMaterialService(
        ISolicitudMaterialRepository solicitudRepository,
        IFichaRepository fichaRepository,
        IProductionOrderRepository orderRepository,
        IMaterialRepository materialRepository,
        IBodegaRepository bodegaRepository,
        ICodigoGeneradorService codigoGenerador,
        IAlertService alertService,
        IUnitOfWork unitOfWork)
    {
        _solicitudRepository = solicitudRepository;
        _fichaRepository = fichaRepository;
        _orderRepository = orderRepository;
        _materialRepository = materialRepository;
        _bodegaRepository = bodegaRepository;
        _codigoGenerador = codigoGenerador;
        _alertService = alertService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> CreateAsync(
        CreateSolicitudMaterialDto dto,
        int solicitanteId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default)
    {
        if (solicitanteId <= 0)
            return ServiceResult.Fail("Usuario no válido.");

        if (!Enum.IsDefined(dto.Tipo))
            return ServiceResult.Fail("Tipo de solicitud no válido.");

        // Validación EXPLÍCITA por Tipo (no implícita): PorFicha exige FichaId + MaterialId.
        if (dto.Tipo == SolicitudMaterialTipo.PorFicha)
            return await CreatePorFichaAsync(dto, solicitanteId, actorRole, actorName, cancellationToken);

        if (dto.Tipo == SolicitudMaterialTipo.InsumosLibres)
            return await CreateInsumosLibresAsync(dto, solicitanteId, actorRole, actorName, cancellationToken);

        return ServiceResult.Fail("Tipo de solicitud no válido.");
    }

    private async Task<ServiceResult> CreatePorFichaAsync(
        CreateSolicitudMaterialDto dto,
        int solicitanteId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken)
    {
        if (dto.FichaId is not int fichaId || fichaId <= 0)
            return ServiceResult.Fail("La ficha SENA es obligatoria para solicitudes por ficha.");

        var lineas = (dto.Detalles ?? [])
            .Where(d => d.MaterialId is > 0 && d.CantidadSolicitada > 0)
            .GroupBy(d => d.MaterialId!.Value)
            .Select(g => new CreateDetalleSolicitudDto(g.Key, g.Sum(x => x.CantidadSolicitada)))
            .ToList();

        if (lineas.Count == 0)
            return ServiceResult.Fail("Agregue al menos un material con cantidad mayor a cero.");

        if (lineas.Any(l => l.MaterialId is null or <= 0))
            return ServiceResult.Fail("Cada línea de solicitud por ficha debe tener un material del catálogo.");

        var bodegaError = await ValidateBodegaAsync(dto.BodegaId, cancellationToken);
        if (bodegaError is not null)
            return bodegaError;

        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha is null)
            return ServiceResult.Fail("Ficha no encontrada.");

        if (!CanRequestOnFicha(ficha, solicitanteId, actorRole, actorName))
            return ServiceResult.Fail("No tiene permiso para solicitar materiales en esta ficha.");

        foreach (var linea in lineas)
        {
            var material = await _materialRepository.GetByIdAsync(linea.MaterialId!.Value, cancellationToken);
            if (material is null)
                return ServiceResult.Fail("Uno de los materiales seleccionados no existe.");
            if (material.BodegaId != dto.BodegaId)
                return ServiceResult.Fail($"El material '{material.Name}' no pertenece a la bodega seleccionada.");
        }

        int? productionOrderId = null;
        if (dto.ProductionOrderId is int oid && oid > 0)
        {
            var order = await _orderRepository.GetByIdAsync(oid, cancellationToken);
            if (order is null)
                return ServiceResult.Fail("Orden de producción no encontrada.");
            productionOrderId = oid;
        }

        var codigo = await _codigoGenerador.GenerarCodigoSolicitudMaterialAsync(cancellationToken);
        var observaciones = NormalizeOptional(dto.Observaciones);

        var solicitud = new SolicitudMaterial
        {
            Codigo = codigo,
            Tipo = SolicitudMaterialTipo.PorFicha,
            FichaId = ficha.Id,
            ProductionOrderId = productionOrderId,
            DescripcionLibre = null,
            SolicitanteId = solicitanteId,
            Estado = SolicitudMaterialEstado.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            Observaciones = observaciones,
            BodegaId = dto.BodegaId,
            Detalles = lineas.Select(l => new DetalleSolicitudMaterial
            {
                MaterialId = l.MaterialId,
                DescripcionItem = null,
                CantidadSolicitada = l.CantidadSolicitada,
                CantidadAprobada = null,
                EstadoItem = DetalleSolicitudEstado.Pendiente
            }).ToList()
        };

        await _solicitudRepository.AddAsync(solicitud, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _alertService.NotifyUsersAsync(
            AlertType.SolicitudMaterialNueva,
            $"SIPITEX · Nueva solicitud {codigo}",
            $"Se creó la solicitud {codigo} para la ficha {ficha.FichaCode} ({lineas.Count} material(es)).\n\nRevise Solicitudes de materiales en bodega.",
            userIds: null,
            role: UserRoles.Bodeguero,
            cancellationToken);

        return ServiceResult.Ok($"Solicitud {codigo} creada.");
    }

    private async Task<ServiceResult> CreateInsumosLibresAsync(
        CreateSolicitudMaterialDto dto,
        int solicitanteId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin(actorRole) && !IsInstructor(actorRole, solicitanteId))
            return ServiceResult.Fail("No tiene permiso para solicitar insumos.");

        var bodegaError = await ValidateBodegaAsync(dto.BodegaId, cancellationToken);
        if (bodegaError is not null)
            return bodegaError;

        var lineas = (dto.Detalles ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d.DescripcionItem) && d.CantidadSolicitada > 0)
            .Select(d => new CreateDetalleSolicitudDto(
                null,
                d.CantidadSolicitada,
                d.DescripcionItem!.Trim()))
            .ToList();

        if (lineas.Count == 0)
            return ServiceResult.Fail("Agregue al menos un ítem con descripción y cantidad mayor a cero.");

        int? fichaId = null;
        string? fichaCode = null;
        if (dto.FichaId is int fid && fid > 0)
        {
            var ficha = await _fichaRepository.GetByIdAsync(fid, cancellationToken);
            if (ficha is null)
                return ServiceResult.Fail("Ficha no encontrada.");
            if (!CanRequestOnFicha(ficha, solicitanteId, actorRole, actorName))
                return ServiceResult.Fail("No tiene permiso para vincular esta ficha a la solicitud.");
            fichaId = ficha.Id;
            fichaCode = ficha.FichaCode;
        }

        int? productionOrderId = null;
        if (dto.ProductionOrderId is int oid && oid > 0)
        {
            var order = await _orderRepository.GetByIdAsync(oid, cancellationToken);
            if (order is null)
                return ServiceResult.Fail("Orden de producción no encontrada.");
            productionOrderId = oid;
        }

        var codigo = await _codigoGenerador.GenerarCodigoSolicitudMaterialAsync(cancellationToken);
        var descripcionLibre = NormalizeOptional(dto.DescripcionLibre, maxLen: 2000);
        var observaciones = NormalizeOptional(dto.Observaciones);

        var solicitud = new SolicitudMaterial
        {
            Codigo = codigo,
            Tipo = SolicitudMaterialTipo.InsumosLibres,
            FichaId = fichaId,
            ProductionOrderId = productionOrderId,
            DescripcionLibre = descripcionLibre,
            SolicitanteId = solicitanteId,
            Estado = SolicitudMaterialEstado.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            Observaciones = observaciones,
            BodegaId = dto.BodegaId,
            Detalles = lineas.Select(l => new DetalleSolicitudMaterial
            {
                MaterialId = null,
                DescripcionItem = l.DescripcionItem,
                CantidadSolicitada = l.CantidadSolicitada,
                CantidadAprobada = null,
                EstadoItem = DetalleSolicitudEstado.Pendiente
            }).ToList()
        };

        await _solicitudRepository.AddAsync(solicitud, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var contexto = fichaCode is not null
            ? $" (ficha {fichaCode})"
            : productionOrderId is not null
                ? $" (orden #{productionOrderId})"
                : string.Empty;

        await _alertService.NotifyUsersAsync(
            AlertType.SolicitudMaterialNueva,
            $"SIPITEX · Nueva solicitud {codigo}",
            $"Se creó la solicitud {codigo} de insumos libres{contexto} ({lineas.Count} ítem(s) por descripción).\n\nRevise Solicitudes de materiales en bodega.",
            userIds: null,
            role: UserRoles.Bodeguero,
            cancellationToken);

        return ServiceResult.Ok($"Solicitud {codigo} creada.");
    }

    public async Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListAsync(
        int? viewerUserId,
        string? viewerRole,
        CancellationToken cancellationToken = default)
    {
        var all = await _solicitudRepository.GetAllWithFichaAsync(cancellationToken);

        IEnumerable<SolicitudMaterial> scoped = all;
        if (IsInstructor(viewerRole, viewerUserId))
            scoped = all.Where(s => s.SolicitanteId == viewerUserId);
        else if (!IsAdmin(viewerRole))
            scoped = [];

        return scoped.Select(MapListItem).ToList();
    }

    public async Task<SolicitudMaterialDetailDto?> GetDetailAsync(
        int id,
        int? viewerUserId,
        string? viewerRole,
        CancellationToken cancellationToken = default)
    {
        var solicitud = await _solicitudRepository.GetByIdWithDetallesAsync(id, cancellationToken);
        if (solicitud is null)
            return null;

        if (IsInstructor(viewerRole, viewerUserId) && solicitud.SolicitanteId != viewerUserId)
            return null;

        if (!IsAdmin(viewerRole) && !IsInstructor(viewerRole, viewerUserId))
            return null;

        return MapDetail(solicitud);
    }

    public async Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListForBodegaAsync(
        int bodegaId,
        bool soloPendientes = true,
        CancellationToken cancellationToken = default)
    {
        var all = await _solicitudRepository.GetAllWithFichaAsync(cancellationToken);
        IEnumerable<SolicitudMaterial> scoped = all.Where(s => s.BodegaId == bodegaId);
        if (soloPendientes)
            scoped = scoped.Where(s => s.Estado == SolicitudMaterialEstado.Pendiente);

        return scoped.Select(MapListItem).ToList();
    }

    public async Task<SolicitudMaterialResolucionDto?> GetResolucionDetailAsync(
        int id,
        int bodegaId,
        CancellationToken cancellationToken = default)
    {
        var solicitud = await _solicitudRepository.GetByIdWithDetallesAsync(id, cancellationToken);
        if (solicitud is null)
            return null;

        if (solicitud.BodegaId != bodegaId)
            return null;

        return new SolicitudMaterialResolucionDto(
            solicitud.Id,
            solicitud.Codigo,
            solicitud.Tipo,
            solicitud.Ficha?.FichaCode ?? "—",
            solicitud.DescripcionLibre,
            solicitud.Solicitante?.Nombre ?? "—",
            solicitud.Estado,
            solicitud.FechaSolicitud,
            solicitud.Observaciones,
            solicitud.Entrega?.Codigo,
            solicitud.Detalles
                .OrderBy(d => d.Id)
                .Select(d => new DetalleResolucionDto(
                    d.Id,
                    d.Material?.Name ?? "—",
                    d.DescripcionItem,
                    d.Material is null ? "—" : UnitHelper.ToDisplay(d.Material.Unit),
                    d.CantidadSolicitada,
                    d.Material?.Stock ?? 0,
                    d.CantidadAprobada,
                    d.EstadoItem))
                .ToList());
    }

    private async Task<ServiceResult?> ValidateBodegaAsync(int bodegaId, CancellationToken cancellationToken)
    {
        if (bodegaId <= 0)
            return ServiceResult.Fail("Bodega no válida.");

        var bodega = await _bodegaRepository.GetByIdAsync(bodegaId, cancellationToken);
        if (bodega is null)
            return ServiceResult.Fail("Bodega no válida.");

        return null;
    }

    private static SolicitudMaterialListItemDto MapListItem(SolicitudMaterial s) =>
        new(s.Id, s.Codigo, s.Tipo, s.Ficha?.FichaCode ?? "—", s.Estado, s.FechaSolicitud, s.Solicitante?.Nombre ?? "—");

    private static SolicitudMaterialDetailDto MapDetail(SolicitudMaterial solicitud) =>
        new(
            solicitud.Id,
            solicitud.Codigo,
            solicitud.Tipo,
            solicitud.Ficha?.FichaCode ?? "—",
            solicitud.DescripcionLibre,
            solicitud.Solicitante?.Nombre ?? "—",
            solicitud.Estado,
            solicitud.FechaSolicitud,
            solicitud.FechaResolucion,
            solicitud.Observaciones,
            solicitud.Detalles
                .OrderBy(d => d.Id)
                .Select(d => new DetalleSolicitudMaterialDto(
                    d.Id,
                    d.Material?.Name ?? "—",
                    d.DescripcionItem,
                    d.Material is null ? "—" : UnitHelper.ToDisplay(d.Material.Unit),
                    d.CantidadSolicitada,
                    d.CantidadAprobada,
                    d.EstadoItem))
                .ToList());

    private static bool CanRequestOnFicha(
        Ficha ficha,
        int actorUserId,
        string? actorRole,
        string? actorName)
    {
        if (IsAdmin(actorRole))
            return true;

        return IsInstructor(actorRole, actorUserId)
               && BelongsToInstructor(ficha, actorUserId, actorName);
    }

    private static bool BelongsToInstructor(Ficha ficha, int instructorUserId, string? instructorName)
    {
        if (ficha.Instructors.Any(i => i.UserId == instructorUserId))
            return true;

        if (ficha.InstructorUserId == instructorUserId)
            return true;

        return ficha.InstructorUserId is null
               && ficha.Instructors.Count == 0
               && !string.IsNullOrWhiteSpace(instructorName)
               && string.Equals(ficha.InstructorName, instructorName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value, int maxLen = 500)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
    }

    private static bool IsAdmin(string? role) =>
        string.Equals(role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);

    private static bool IsInstructor(string? role, int? userId) =>
        userId is > 0
        && string.Equals(role, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);
}
