using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Órdenes de producción y registro de avance
public interface IProductionOrderService
{
    // viewer*: si el rol es Instructor, solo devuelve órdenes asignadas a ese usuario
    Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    // Admin/Bodeguero: siempre. Instructor: solo si es responsable asignado (etapa o ficha).
    Task<bool> CanAccessOrderAsync(
        int orderId,
        int? viewerUserId,
        string? viewerRole,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> CreateOrderAsync(CreateProductionOrderDto dto, CancellationToken cancellationToken = default);
    // units = unidades producidas en este registro (descuenta BOM)
    Task<ServiceResult> RegisterProductionAsync(int orderId, int units, CancellationToken cancellationToken = default);
}
