using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IProductionOrderService
{
    Task<IReadOnlyList<ProductionOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateOrderAsync(CreateProductionOrderDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> RegisterProductionAsync(int orderId, int units, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetKnownProductNamesAsync(CancellationToken cancellationToken = default);
}
