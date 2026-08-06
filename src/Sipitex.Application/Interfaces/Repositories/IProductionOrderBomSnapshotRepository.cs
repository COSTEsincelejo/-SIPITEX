using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Receta congelada por orden de producción
public interface IProductionOrderBomSnapshotRepository
{
    Task<IReadOnlyList<ProductionOrderBomSnapshot>> GetByOrderIdAsync(int productionOrderId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ProductionOrderBomSnapshot> snapshots, CancellationToken cancellationToken = default);
}
