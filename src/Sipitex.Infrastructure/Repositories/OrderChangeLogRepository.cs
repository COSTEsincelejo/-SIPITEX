using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class OrderChangeLogRepository : IOrderChangeLogRepository
{
    private readonly SipitexDbContext _db;

    public OrderChangeLogRepository(SipitexDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<OrderChangeLog> entries, CancellationToken cancellationToken = default) =>
        await _db.OrderChangeLogs.AddRangeAsync(entries, cancellationToken);

    public async Task<IReadOnlyList<OrderChangeLog>> GetByOrderIdAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default) =>
        await _db.OrderChangeLogs
            .AsNoTracking()
            .Include(c => c.Usuario)
            .Where(c => c.ProductionOrderId == productionOrderId)
            .OrderByDescending(c => c.FechaUtc)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
}
