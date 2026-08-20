using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Aprueba / resuelve SolicitudMaterial descontando stock de forma atómica
public class SolicitudMaterialApprovalService : ISolicitudMaterialApprovalService
{
    private readonly ISolicitudMaterialRepository _solicitudRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IStockMovementRepository _stockMovements;
    private readonly ICodigoGeneradorService _codigoGenerador;
    private readonly IAlertService _alertService;
    private readonly IUnitOfWork _unitOfWork;

    public SolicitudMaterialApprovalService(
        ISolicitudMaterialRepository solicitudRepository,
        IMaterialRepository materialRepository,
        IStockMovementRepository stockMovements,
        ICodigoGeneradorService codigoGenerador,
        IAlertService alertService,
        IUnitOfWork unitOfWork)
    {
        _solicitudRepository = solicitudRepository;
        _materialRepository = materialRepository;
        _stockMovements = stockMovements;
        _codigoGenerador = codigoGenerador;
        _alertService = alertService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> ApproveDetalleAsync(
        int detalleId,
        decimal cantidadAprobada,
        int resueltoPorId,
        CancellationToken cancellationToken = default)
    {
        if (cantidadAprobada <= 0)
            return ServiceResult.Fail("La cantidad aprobada debe ser mayor que cero.");

        var detalle = await _solicitudRepository.GetDetalleByIdAsync(detalleId, cancellationToken);
        if (detalle is null)
            return ServiceResult.Fail("Detalle de solicitud no encontrado.");

        if (detalle.EstadoItem != DetalleSolicitudEstado.Pendiente)
            return ServiceResult.Fail("El ítem ya fue resuelto.");

        var error = ValidarCantidadAprobada(detalle, cantidadAprobada);
        if (error is not null)
            return ServiceResult.Fail(error);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var (applyError, movement) = AplicarDecisionDetalle(detalle, cantidadAprobada, resueltoPorId);
                if (applyError is not null)
                    throw new InvalidOperationException(applyError);

                if (detalle.Material is not null)
                    _materialRepository.Update(detalle.Material);
                if (movement is not null)
                    await _stockMovements.AddAsync(movement, ct);

                ActualizarEstadoSolicitud(detalle.SolicitudMaterial, resueltoPorId);
                _solicitudRepository.Update(detalle.SolicitudMaterial);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.Fail(ex.Message);
        }

        return ServiceResult.Ok("Ítem aprobado.");
    }

    public async Task<ServiceResult> ResolveSolicitudAsync(
        int solicitudId,
        IReadOnlyList<ResolveDetalleDto> items,
        int bodegueroId,
        string? observaciones = null,
        CancellationToken cancellationToken = default)
    {
        if (bodegueroId <= 0)
            return ServiceResult.Fail("Bodeguero no válido.");

        var solicitud = await _solicitudRepository.GetByIdWithDetallesAsync(solicitudId, cancellationToken);
        if (solicitud is null)
            return ServiceResult.Fail("Solicitud no encontrada.");

        if (solicitud.Estado != SolicitudMaterialEstado.Pendiente)
            return ServiceResult.Fail("La solicitud ya fue resuelta y no puede modificarse.");

        var decisions = (items ?? [])
            .GroupBy(i => i.DetalleId)
            .ToDictionary(g => g.Key, g => g.Last());

        if (solicitud.Detalles.Count == 0)
            return ServiceResult.Fail("La solicitud no tiene ítems.");

        // Mapear ítems sin MaterialId (InsumosLibres) ANTES de validar/descontar stock.
        foreach (var detalle in solicitud.Detalles)
        {
            if (detalle.EstadoItem != DetalleSolicitudEstado.Pendiente)
                return ServiceResult.Fail("Hay ítems ya resueltos; no se puede re-resolver.");

            if (!decisions.TryGetValue(detalle.Id, out var decision))
                return ServiceResult.Fail("Debe indicar una cantidad aprobada para cada material.");

            if (decision.CantidadAprobada < 0)
                return ServiceResult.Fail("La cantidad aprobada no puede ser negativa.");

            if (decision.CantidadAprobada > 0)
            {
                var mapError = await EnsureDetalleMappedAsync(
                    detalle, decision, solicitud.BodegaId, cancellationToken);
                if (mapError is not null)
                    return ServiceResult.Fail(mapError);

                var error = ValidarCantidadAprobada(detalle, decision.CantidadAprobada);
                if (error is not null)
                    return ServiceResult.Fail(error);
            }
        }

        string? entregaCodigo = null;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var movements = new List<StockMovement>();
                foreach (var detalle in solicitud.Detalles)
                {
                    var cantidad = decisions[detalle.Id].CantidadAprobada;
                    var (applyError, movement) = AplicarDecisionDetalle(detalle, cantidad, bodegueroId);
                    if (applyError is not null)
                        throw new InvalidOperationException(applyError);

                    if (cantidad > 0 && detalle.Material is not null)
                        _materialRepository.Update(detalle.Material);

                    if (movement is not null)
                        movements.Add(movement);
                }

                if (movements.Count > 0)
                    await _stockMovements.AddRangeAsync(movements, ct);

                if (!string.IsNullOrWhiteSpace(observaciones))
                    solicitud.Observaciones = observaciones.Trim();

                ActualizarEstadoSolicitud(solicitud, bodegueroId);

                var hayEntrega = solicitud.Detalles.Any(d => (d.CantidadAprobada ?? 0) > 0);
                if (hayEntrega)
                {
                    entregaCodigo = await _codigoGenerador.GenerarCodigoEntregaMaterialAsync(ct);
                    await _solicitudRepository.AddEntregaAsync(new EntregaMaterial
                    {
                        Codigo = entregaCodigo,
                        SolicitudMaterialId = solicitud.Id,
                        BodegueroId = bodegueroId,
                        FechaEntrega = DateTime.UtcNow,
                        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim()
                    }, ct);
                }

                _solicitudRepository.Update(solicitud);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.Fail(ex.Message);
        }

        // Notificación fuera de la transacción (tras commit exitoso)
        var estadoTexto = DisplayEstado(solicitud.Estado);
        var body = $"Su solicitud {solicitud.Codigo} fue resuelta: {estadoTexto}.";
        if (!string.IsNullOrWhiteSpace(entregaCodigo))
            body += $"\nEntrega generada: {entregaCodigo}.";
        body += "\n\nRevise el detalle en Mis solicitudes.";

        await _alertService.NotifyUsersAsync(
            AlertType.SolicitudMaterialResuelta,
            $"SIPITEX · Solicitud {solicitud.Codigo} resuelta ({estadoTexto})",
            body,
            userIds: [solicitud.SolicitanteId],
            role: null,
            cancellationToken);

        return ServiceResult.Ok(
            string.IsNullOrWhiteSpace(entregaCodigo)
                ? $"Solicitud {solicitud.Codigo} resuelta ({DisplayEstado(solicitud.Estado)})."
                : $"Solicitud {solicitud.Codigo} resuelta. Entrega {entregaCodigo}.");
    }

    // Asigna MaterialId real (existente o creado inline) antes de cualquier descuento.
    private async Task<string?> EnsureDetalleMappedAsync(
        DetalleSolicitudMaterial detalle,
        ResolveDetalleDto decision,
        int solicitudBodegaId,
        CancellationToken cancellationToken)
    {
        if (detalle.MaterialId is > 0 && detalle.Material is not null)
        {
            if (detalle.Material.BodegaId != solicitudBodegaId)
                return "El material seleccionado pertenece a otra bodega.";
            return null;
        }

        if (decision.MaterialId is int existingId && existingId > 0)
        {
            var material = await _materialRepository.GetByIdAsync(existingId, cancellationToken);
            if (material is null)
                return "El material seleccionado para mapeo no existe.";
            if (material.BodegaId != solicitudBodegaId)
                return "El material seleccionado pertenece a otra bodega.";
            detalle.MaterialId = material.Id;
            detalle.Material = material;
            return null;
        }

        if (!string.IsNullOrWhiteSpace(decision.NewMaterialName) && decision.NewMaterialUnit is not null)
        {
            if (!Enum.IsDefined(decision.NewMaterialUnit.Value))
                return "Unidad no válida para el material nuevo.";

            var created = new Material
            {
                Code = $"mat{DateTime.UtcNow.Ticks}",
                Name = decision.NewMaterialName.Trim(),
                Unit = decision.NewMaterialUnit.Value,
                Stock = 0,
                MinStock = 0,
                Status = MaterialStatus.Bueno,
                LastEntryDate = DateOnly.FromDateTime(DateTime.Today),
                BodegaId = solicitudBodegaId
            };
            // Seguimiento: SaveChanges aquí queda fuera de ExecuteInTransactionAsync del Resolve.
            // Si la transacción posterior falla, el Material (Stock=0) puede quedar huérfano.
            // Futuro: mover la creación dentro de la misma transacción.
            await _materialRepository.AddAsync(created, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            detalle.MaterialId = created.Id;
            detalle.Material = created;
            return null;
        }

        return "Debe mapear el ítem a un material del catálogo (o crear uno) antes de descontar stock.";
    }

    // Validación autoritativa: CantidadAprobada <= min(Solicitada, Stock)
    internal static string? ValidarCantidadAprobada(DetalleSolicitudMaterial detalle, decimal cantidadAprobada)
    {
        // Sin MaterialId mapeado no se puede validar ni descontar stock (InsumosLibres).
        if (detalle.MaterialId is null || detalle.Material is null)
            return "Debe mapear el ítem a un material del catálogo antes de descontar stock.";

        if (cantidadAprobada > detalle.CantidadSolicitada)
            return "La cantidad aprobada no puede superar la solicitada.";

        if (cantidadAprobada > detalle.Material.Stock)
            return "Stock insuficiente para aprobar la cantidad indicada.";

        return null;
    }

    // Aplica decisión a un ítem (0 = rechazo sin stock; >0 = descuento). Reutilizado por Approve y Resolve.
    // También arma el StockMovement cuando hay descuento (gap #15 auditoría).
    internal static (string? Error, StockMovement? Movement) AplicarDecisionDetalle(
        DetalleSolicitudMaterial detalle,
        decimal cantidadAprobada,
        int actorUserId)
    {
        if (detalle.EstadoItem != DetalleSolicitudEstado.Pendiente)
            return ("El ítem ya fue resuelto.", null);

        if (cantidadAprobada < 0)
            return ("La cantidad aprobada no puede ser negativa.", null);

        if (cantidadAprobada == 0)
        {
            detalle.CantidadAprobada = 0;
            detalle.EstadoItem = DetalleSolicitudEstado.Rechazado;
            return (null, null);
        }

        var error = ValidarCantidadAprobada(detalle, cantidadAprobada);
        if (error is not null)
            return (error, null);

        // Revalidación de stock en el momento de aplicar (carrera)
        if (cantidadAprobada > detalle.Material!.Stock)
            return ("Stock insuficiente para aprobar la cantidad indicada.", null);

        detalle.Material.Stock -= cantidadAprobada;
        detalle.CantidadAprobada = cantidadAprobada;
        detalle.EstadoItem = cantidadAprobada == detalle.CantidadSolicitada
            ? DetalleSolicitudEstado.Aprobado
            : DetalleSolicitudEstado.AprobadoParcial;

        var movement = new StockMovement
        {
            MaterialId = detalle.MaterialId!.Value,
            FechaUtc = DateTime.UtcNow,
            UsuarioId = actorUserId,
            TipoMovimiento = StockMovementType.AprobacionSolicitud,
            Cantidad = cantidadAprobada,
            StockResultante = detalle.Material.Stock,
            Referencia = $"SolicitudMaterial:{detalle.SolicitudMaterialId}"
        };

        return (null, movement);
    }

    // Recalcula Estado global; FechaResolucion solo cuando no quedan ítems Pendiente
    internal static void ActualizarEstadoSolicitud(SolicitudMaterial solicitud, int resueltoPorId)
    {
        var detalles = solicitud.Detalles;
        if (detalles.Count == 0)
        {
            solicitud.Estado = SolicitudMaterialEstado.Pendiente;
            return;
        }

        var hayPendiente = detalles.Any(d => d.EstadoItem == DetalleSolicitudEstado.Pendiente);
        if (hayPendiente)
        {
            solicitud.Estado = SolicitudMaterialEstado.Pendiente;
            return;
        }

        var todosRechazados = detalles.All(d => d.EstadoItem == DetalleSolicitudEstado.Rechazado);
        var todosAprobadosTotales = detalles.All(d => d.EstadoItem == DetalleSolicitudEstado.Aprobado);

        if (todosRechazados)
            solicitud.Estado = SolicitudMaterialEstado.Rechazada;
        else if (todosAprobadosTotales)
            solicitud.Estado = SolicitudMaterialEstado.AprobadaTotal;
        else
            solicitud.Estado = SolicitudMaterialEstado.AprobadaParcial;

        solicitud.FechaResolucion = DateTime.UtcNow;
        solicitud.ResueltoPorId = resueltoPorId;
    }

    private static string DisplayEstado(SolicitudMaterialEstado estado) => estado switch
    {
        SolicitudMaterialEstado.AprobadaTotal => "Aprobada total",
        SolicitudMaterialEstado.AprobadaParcial => "Aprobada parcial",
        SolicitudMaterialEstado.Rechazada => "Rechazada",
        _ => estado.ToString()
    };
}
