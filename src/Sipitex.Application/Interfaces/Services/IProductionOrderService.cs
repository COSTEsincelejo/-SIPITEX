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

    // Admin/Bodeguero: siempre. Instructor: solo si es responsable asignado (etapa MES o ficha).
    Task<bool> CanAccessOrderAsync(
        int orderId,
        int? viewerUserId,
        string? viewerRole,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    // Gate independiente de materiales: Admin siempre; Instructor si ∈ BomProductInstructor
    // del producto O ∈ etapa MES de la orden. No abre producción/MES.
    Task<ServiceResult> AuthorizeOrderMaterialsAsync(
        int orderId,
        int? viewerUserId,
        string? viewerRole,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> CreateOrderAsync(CreateProductionOrderDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> ApproveOrderAsync(int orderId, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateOrderAsync(UpdateProductionOrderDto dto, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelOrderAsync(int orderId, int actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderChangeLogDto>> GetChangeLogAsync(int orderId, CancellationToken cancellationToken = default);
    // units = unidades producidas en este registro (descuenta BOM)
    Task<ServiceResult> RegisterProductionAsync(int orderId, int units, CancellationToken cancellationToken = default);
}
