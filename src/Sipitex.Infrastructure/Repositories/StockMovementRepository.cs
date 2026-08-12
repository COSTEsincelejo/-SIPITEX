using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly SipitexDbContext _db;

    public StockMovementRepository(SipitexDbContext db) => _db = db;

    public async Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default) =>
        await _db.StockMovements.AddAsync(movement, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<StockMovement> movements, CancellationToken cancellationToken = default) =>
        await _db.StockMovements.AddRangeAsync(movements, cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> QueryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        int? materialId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockMovements
            .AsNoTracking()
            .Include(m => m.Material)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (fromUtc is DateTime from)
            query = query.Where(m => m.FechaUtc >= from);

        if (toUtc is DateTime to)
            query = query.Where(m => m.FechaUtc <= to);

        if (materialId is int mid and > 0)
            query = query.Where(m => m.MaterialId == mid);

        return await query
            .OrderByDescending(m => m.FechaUtc)
            .ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);
    }
}
