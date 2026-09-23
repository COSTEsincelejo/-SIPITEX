using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class ActaMovimientoRepository : IActaMovimientoRepository
{
    private readonly SipitexDbContext _db;

    public ActaMovimientoRepository(SipitexDbContext db) => _db = db;

    public async Task AddAsync(ActaMovimiento acta, CancellationToken cancellationToken = default) =>
        await _db.ActasMovimiento.AddAsync(acta, cancellationToken);

    public Task<ActaMovimiento?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ActasMovimiento
            .Include(a => a.Detalles)
                .ThenInclude(d => d.Material)
            .Include(a => a.ProductionOrder)
            .Include(a => a.CreadoPor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ActaMovimiento>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.ActasMovimiento
            .AsNoTracking()
            .Include(a => a.Detalles)
                .ThenInclude(d => d.Material)
            .Include(a => a.ProductionOrder)
            .Include(a => a.CreadoPor)
            .OrderByDescending(a => a.FechaUtc)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<ActaMovimiento> Items, int TotalCount, int Page)> PageAsync(
        string? role,
        int userId,
        IReadOnlyCollection<int> plantaInventarioIds,
        IReadOnlyCollection<int> allowedOrderIds,
        int? page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var size = pageSize < 1 ? Paging.DefaultPageSize : pageSize;
        var query = _db.ActasMovimiento.AsNoTracking().AsQueryable();
        if (string.Equals(role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
        {
        }
        else if (string.Equals(role, UserRoles.EncargadoDeBodega, StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a =>
                a.CreadoPorUserId == userId
                || a.Detalles.Any(d =>
                    d.Material != null && plantaInventarioIds.Contains(d.Material.PlantaInventarioId)));
        }
        else if (string.Equals(role, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a =>
                a.CreadoPorUserId == userId
                || (a.ProductionOrderId != null && allowedOrderIds.Contains(a.ProductionOrderId.Value)));
        }
        else
        {
            query = query.Where(a => false);
        }

        var total = await query.CountAsync(cancellationToken);
        var current = Paging.ClampPage(page, total, size);
        var items = await query
            .Include(a => a.Detalles)
                .ThenInclude(d => d.Material)
            .Include(a => a.ProductionOrder)
            .Include(a => a.CreadoPor)
            .OrderByDescending(a => a.FechaUtc)
            .ThenByDescending(a => a.Id)
            .Skip((current - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);
        return (items, total, current);
    }

    public async Task<string?> GetLastNumeroAsync(CancellationToken cancellationToken = default) =>
        await _db.ActasMovimiento
            .AsNoTracking()
            .OrderByDescending(a => a.Id)
            .Select(a => a.Numero)
            .FirstOrDefaultAsync(cancellationToken);
}
