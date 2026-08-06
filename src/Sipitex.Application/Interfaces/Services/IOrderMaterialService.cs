using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Materiales opcionales de una orden + entrega desde bodega
public interface IOrderMaterialService
{
    Task<OrderMaterialsDetailDto?> GetDetailAsync(int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionOrderDto>> GetOrdersForBodegaAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult> AddMaterialAsync(AddOrderMaterialDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> RemoveMaterialAsync(int lineId, CancellationToken cancellationToken = default);

    // Sugiere líneas desde el snapshot BOM × cantidad de la orden (sin sobrescribir entregas)
    Task<ServiceResult> ImportFromBomAsync(int orderId, CancellationToken cancellationToken = default);

    Task<ServiceResult> ValidateStockAsync(int orderId, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeliverAsync(
        DeliverOrderMaterialsDto dto,
        int bodegueroId,
        CancellationToken cancellationToken = default);
}
