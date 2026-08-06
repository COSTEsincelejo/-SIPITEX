using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Aprobación / resolución de SolicitudMaterial con descuento atómico de stock
public interface ISolicitudMaterialApprovalService
{
    // Aprueba un detalle: descuenta CantidadAprobada del Stock del Material dentro de una transacción.
    // Si CantidadAprobada > Stock, falla sin modificar detalle ni inventario.
    Task<ServiceResult> ApproveDetalleAsync(
        int detalleId,
        decimal cantidadAprobada,
        int resueltoPorId,
        CancellationToken cancellationToken = default);

    // Resuelve toda la solicitud en una transacción (ítems + estado + entrega opcional + notificación).
    // Precondición: Estado == Pendiente.
    Task<ServiceResult> ResolveSolicitudAsync(
        int solicitudId,
        IReadOnlyList<ResolveDetalleDto> items,
        int bodegueroId,
        string? observaciones = null,
        CancellationToken cancellationToken = default);
}
