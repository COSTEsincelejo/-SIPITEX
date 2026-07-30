using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Órdenes de producción y registro de avance
public interface IProductionOrderService
{
    Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateOrderAsync(CreateProductionOrderDto dto, CancellationToken cancellationToken = default);
    // units = unidades producidas en este registro (descuenta BOM)
    Task<ServiceResult> RegisterProductionAsync(int orderId, int units, CancellationToken cancellationToken = default);
}
