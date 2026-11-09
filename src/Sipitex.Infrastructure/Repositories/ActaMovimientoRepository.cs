using Microsoft.EntityFrameworkCore;
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
                .ThenInclude(d => d.Material!)
                    .ThenInclude(m => m.PlantaInventario)
            .Include(a => a.ProductionOrder)
            .Include(a => a.CreadoPor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ActaMovimiento>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.ActasMovimiento
            .AsNoTracking()
            .Include(a => a.Detalles)
                .ThenInclude(d => d.Material!)
                    .ThenInclude(m => m.PlantaInventario)
            .Include(a => a.ProductionOrder)
            .Include(a => a.CreadoPor)
            .OrderByDescending(a => a.FechaUtc)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<string?> GetLastNumeroAsync(CancellationToken cancellationToken = default) =>
        await _db.ActasMovimiento
            .AsNoTracking()
            .OrderByDescending(a => a.Id)
            .Select(a => a.Numero)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, string>> GetNumerosPorStockMovementAsync(CancellationToken cancellationToken = default)
    {
        var filas = await _db.ActasMovimientoDetalle.AsNoTracking()
            .Where(d => d.StockMovementId != null)
            .Select(d => new { Id = d.StockMovementId!.Value, d.ActaMovimiento.Numero })
            .ToListAsync(cancellationToken);

        return filas
            .GroupBy(f => f.Id)
            .ToDictionary(g => g.Key, g => g.First().Numero);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetNumerosPorConsumoAsync(CancellationToken cancellationToken = default)
    {
        var filas = await _db.ActasMovimientoDetalle.AsNoTracking()
            .Where(d => d.ConsumoMaterialId != null)
            .Select(d => new { Id = d.ConsumoMaterialId!.Value, d.ActaMovimiento.Numero })
            .ToListAsync(cancellationToken);

        return filas
            .GroupBy(f => f.Id)
            .ToDictionary(g => g.Key, g => g.First().Numero);
    }
}
