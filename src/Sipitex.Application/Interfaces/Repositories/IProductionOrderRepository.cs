using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IProductionOrderRepository
{
    Task<IReadOnlyList<ProductionOrder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductionOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task AddAsync(ProductionOrder order, CancellationToken cancellationToken = default);
    void Update(ProductionOrder order);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctProductNamesAsync(CancellationToken cancellationToken = default);
}
