using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Helpers;
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

    public async Task<(IReadOnlyList<PrendaTrazable> Items, int TotalCount, int Page)> ListPageAsync(
        int? productionOrderId,
        string? query,
        IReadOnlyCollection<int>? allowedOrderIds,
        int? page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var size = pageSize < 1 ? Paging.DefaultPageSize : pageSize;
        var rows = Filtered(productionOrderId, query, allowedOrderIds);
        var total = await rows.CountAsync(cancellationToken);
        var current = Paging.ClampPage(page, total, size);
        var items = await rows
            .OrderByDescending(p => p.CreadoUtc)
            .ThenByDescending(p => p.Id)
            .Skip((current - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);
        return (items, total, current);
    }

    private IQueryable<PrendaTrazable> Filtered(
        int? productionOrderId,
        string? query,
        IReadOnlyCollection<int>? allowedOrderIds)
    {
        var q = (query ?? string.Empty).Trim();
        var rows = _db.PrendasTrazables
            .AsNoTracking()
            .Include(p => p.ProductionOrder)
            .Include(p => p.CreadoPor)
            .AsQueryable();

        if (allowedOrderIds is not null)
            rows = rows.Where(p => allowedOrderIds.Contains(p.ProductionOrderId));

        if (productionOrderId is int oid)
            rows = rows.Where(p => p.ProductionOrderId == oid);

        if (q.Length > 0)
        {
            var term = q.ToLower();
            rows = rows.Where(p =>
                p.Codigo.ToLower().Contains(term)
                || p.ProductName.ToLower().Contains(term)
                || p.ProductionOrder.OrderNumber.ToLower().Contains(term));
        }

        return rows;
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
