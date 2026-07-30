using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Órdenes de producción (OP)
public interface IProductionOrderRepository
{
    Task<IReadOnlyList<ProductionOrder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductionOrder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task AddAsync(ProductionOrder order, CancellationToken cancellationToken = default);
    void Update(ProductionOrder order);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
