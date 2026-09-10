using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

// Acceso a la tabla PlantasInventario. Alta y listado para el admin.
public class PlantaInventarioRepository : IPlantaInventarioRepository
{
    private readonly SipitexDbContext _context;

    public PlantaInventarioRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<PlantaInventario>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.PlantasInventario.AsNoTracking().OrderBy(b => b.Nombre).ToListAsync(cancellationToken);

    public Task<PlantaInventario?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PlantasInventario.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsByNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default,
        int? excludeId = null)
    {
        var normalized = nombre.Trim().ToLower();
        var query = _context.PlantasInventario.AsQueryable().Where(b => b.Nombre.ToLower() == normalized);
        if (excludeId is int id)
            query = query.Where(b => b.Id != id);
        return query.AnyAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _context.PlantasInventario.CountAsync(cancellationToken);

    public Task<int> CountActivasAsync(CancellationToken cancellationToken = default) =>
        _context.PlantasInventario.CountAsync(b => b.Activo, cancellationToken);

    public async Task<PlantaInventarioDependencias> CountDependenciasAsync(
        int plantaInventarioId,
        CancellationToken cancellationToken = default)
    {
        var materiales = await _context.Materials.CountAsync(m => m.PlantaInventarioId == plantaInventarioId, cancellationToken);
        var stockTotal = await _context.Materials
            .Where(m => m.PlantaInventarioId == plantaInventarioId)
            .SumAsync(m => (decimal?)m.Stock, cancellationToken) ?? 0;
        var solicitudes = await _context.SolicitudesMaterial.CountAsync(s => s.PlantaInventarioId == plantaInventarioId, cancellationToken);
        var encargados = await _context.UserPlantasInventario.CountAsync(ub => ub.PlantaInventarioId == plantaInventarioId, cancellationToken);
        return new PlantaInventarioDependencias(materiales, solicitudes, encargados, stockTotal);
    }

    public async Task<PlantaInventarioReassignmentResult> ReassignDependenciasAsync(
        int origenId,
        int destinoId,
        CancellationToken cancellationToken = default)
    {
        var materiales = await _context.Materials
            .IgnoreQueryFilters()
            .Where(m => m.PlantaInventarioId == origenId)
            .ToListAsync(cancellationToken);

        var materialIds = materiales.Select(m => m.Id).ToList();
        var stockTotal = materiales.Sum(m => m.Stock);
        foreach (var material in materiales)
            material.PlantaInventarioId = destinoId;

        var movimientos = materialIds.Count == 0
            ? 0
            : await _context.StockMovements
                .IgnoreQueryFilters()
                .CountAsync(m => materialIds.Contains(m.MaterialId), cancellationToken);

        var solicitudes = await _context.SolicitudesMaterial
            .IgnoreQueryFilters()
            .Where(s => s.PlantaInventarioId == origenId)
            .ToListAsync(cancellationToken);
        foreach (var solicitud in solicitudes)
            solicitud.PlantaInventarioId = destinoId;

        var encargados = await _context.UserPlantasInventario
            .Where(ub => ub.PlantaInventarioId == origenId)
            .ToListAsync(cancellationToken);
        var destinoUserIds = (await _context.UserPlantasInventario
            .Where(ub => ub.PlantaInventarioId == destinoId)
            .Select(ub => ub.UserId)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var row in encargados)
        {
            if (destinoUserIds.Add(row.UserId))
            {
                _context.UserPlantasInventario.Add(new UserPlantaInventario
                {
                    UserId = row.UserId,
                    PlantaInventarioId = destinoId
                });
            }
        }
        _context.UserPlantasInventario.RemoveRange(encargados);

        return new PlantaInventarioReassignmentResult(
            materiales.Count,
            movimientos,
            solicitudes.Count,
            encargados.Count,
            stockTotal);
    }

    public async Task AddAsync(PlantaInventario plantaInventario, CancellationToken cancellationToken = default) =>
        await _context.PlantasInventario.AddAsync(plantaInventario, cancellationToken);

    public void Update(PlantaInventario plantaInventario) => _context.PlantasInventario.Update(plantaInventario);

    public void Remove(PlantaInventario plantaInventario) => _context.PlantasInventario.Remove(plantaInventario);
}
