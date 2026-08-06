using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Aprobación (total/parcial) de ítems de SolicitudMaterial con descuento atómico de stock
public interface ISolicitudMaterialApprovalService
{
    // Aprueba un detalle: descuenta CantidadAprobada del Stock del Material dentro de una transacción.
    // Si CantidadAprobada > Stock, falla sin modificar detalle ni inventario.
    Task<ServiceResult> ApproveDetalleAsync(
        int detalleId,
        decimal cantidadAprobada,
        int resueltoPorId,
        CancellationToken cancellationToken = default);
}
