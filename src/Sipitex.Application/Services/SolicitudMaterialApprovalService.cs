using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Aprueba ítems de SolicitudMaterial descontando stock de forma atómica
public class SolicitudMaterialApprovalService : ISolicitudMaterialApprovalService
{
    private readonly ISolicitudMaterialRepository _solicitudRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SolicitudMaterialApprovalService(
        ISolicitudMaterialRepository solicitudRepository,
        IMaterialRepository materialRepository,
        IUnitOfWork unitOfWork)
    {
        _solicitudRepository = solicitudRepository;
        _materialRepository = materialRepository;
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

        if (cantidadAprobada > detalle.CantidadSolicitada)
            return ServiceResult.Fail("La cantidad aprobada no puede superar la solicitada.");

        // Validación previa: si no alcanza stock, no abrimos transacción ni tocamos estado
        if (cantidadAprobada > detalle.Material.Stock)
            return ServiceResult.Fail("Stock insuficiente para aprobar la cantidad indicada.");

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                // Revalidación dentro de la transacción (evita carrera con otra aprobación)
                if (cantidadAprobada > detalle.Material.Stock)
                    throw new InvalidOperationException("Stock insuficiente para aprobar la cantidad indicada.");

                detalle.Material.Stock -= cantidadAprobada;
                detalle.CantidadAprobada = cantidadAprobada;
                detalle.EstadoItem = cantidadAprobada == detalle.CantidadSolicitada
                    ? DetalleSolicitudEstado.Aprobado
                    : DetalleSolicitudEstado.AprobadoParcial;

                _materialRepository.Update(detalle.Material);

                var solicitud = detalle.SolicitudMaterial;
                ActualizarEstadoSolicitud(solicitud, resueltoPorId);
                _solicitudRepository.Update(solicitud);

                await Task.CompletedTask;
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.Fail(ex.Message);
        }

        return ServiceResult.Ok("Ítem aprobado.");
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
            // Mientras queden ítems sin resolver, la solicitud sigue Pendiente
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
}
