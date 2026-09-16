using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class PrendaTrazableRepository : IPrendaTrazableRepository
{
    private readonly SipitexDbContext _db;

    public PrendaTrazableRepository(SipitexDbContext db) => _db = db;

    public async Task AddRangeAsync(IReadOnlyList<PrendaTrazable> items, CancellationToken cancellationToken = default) =>
        await _db.PrendasTrazables.AddRangeAsync(items, cancellationToken);

    public Task<PrendaTrazable?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var normalized = (codigo ?? string.Empty).Trim();
        return _db.PrendasTrazables
            .Include(p => p.ProductionOrder)
            .Include(p => p.CreadoPor)
            .FirstOrDefaultAsync(p => p.Codigo == normalized, cancellationToken);
    }

    public Task<PrendaTrazable?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.PrendasTrazables
            .Include(p => p.ProductionOrder)
            .Include(p => p.CreadoPor)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PrendaTrazable>> ListAsync(
        int? productionOrderId,
        string? query,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        var q = (query ?? string.Empty).Trim();
        var rows = _db.PrendasTrazables
            .AsNoTracking()
            .Include(p => p.ProductionOrder)
            .Include(p => p.CreadoPor)
            .AsQueryable();

        if (productionOrderId is int oid)
            rows = rows.Where(p => p.ProductionOrderId == oid);

        if (q.Length > 0)
        {
            rows = rows.Where(p =>
                p.Codigo.Contains(q)
                || p.ProductName.Contains(q)
                || p.ProductionOrder.OrderNumber.Contains(q));
        }

        return await rows
            .OrderByDescending(p => p.CreadoUtc)
            .ThenByDescending(p => p.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByOrderAsync(int productionOrderId, CancellationToken cancellationToken = default) =>
        _db.PrendasTrazables.CountAsync(p => p.ProductionOrderId == productionOrderId, cancellationToken);

    public async Task<string?> GetLastCodigoForPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        await _db.PrendasTrazables
            .AsNoTracking()
            .Where(p => p.Codigo.StartsWith(prefix))
            .OrderByDescending(p => p.Codigo)
            .Select(p => p.Codigo)
            .FirstOrDefaultAsync(cancellationToken);
}
