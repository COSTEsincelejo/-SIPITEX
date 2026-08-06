using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class ProductionOrderBomSnapshotRepository : IProductionOrderBomSnapshotRepository
{
    private readonly SipitexDbContext _context;

    public ProductionOrderBomSnapshotRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductionOrderBomSnapshot>> GetByOrderIdAsync(int productionOrderId, CancellationToken cancellationToken = default) =>
        await _context.ProductionOrderBomSnapshots
            .Where(s => s.ProductionOrderId == productionOrderId)
            .OrderBy(s => s.MaterialName)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<ProductionOrderBomSnapshot> snapshots, CancellationToken cancellationToken = default) =>
        await _context.ProductionOrderBomSnapshots.AddRangeAsync(snapshots, cancellationToken);
}
