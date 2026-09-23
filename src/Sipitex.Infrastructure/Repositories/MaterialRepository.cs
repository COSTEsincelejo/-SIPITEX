using Microsoft.EntityFrameworkCore; // OrderBy, FirstOrDefaultAsync, AddAsync...
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories; // IMaterialRepository
using Sipitex.Domain.Entities; // Material
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Acceso a la tabla Materials. CRUD básico para el inventario.
public class MaterialRepository : IMaterialRepository
{
    private readonly SipitexDbContext _context;

    public MaterialRepository(SipitexDbContext context) => _context = context;

    // Lista ordenada por nombre para que en la vista se vea alfabético
    public async Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Materials
            .Include(m => m.PlantaInventario)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Material> Items, int TotalCount, int TotalSinFiltro, int Page)> PageAsync(
        int? plantaInventarioId,
        IReadOnlyCollection<int>? allowedPlantaIds,
        string? nombre,
        string? nivel,
        int? page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var size = pageSize < 1 ? Paging.DefaultPageSize : pageSize;
        var scoped = Scope(_context.Materials.AsNoTracking(), plantaInventarioId, allowedPlantaIds);
        var totalSinFiltro = await scoped.CountAsync(cancellationToken);

        var filtered = scoped;
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var term = nombre.Trim().ToLower();
            filtered = filtered.Where(m => m.Name.ToLower().Contains(term));
        }

        filtered = ApplyNivel(filtered, nivel);
        var total = await filtered.CountAsync(cancellationToken);
        var current = Paging.ClampPage(page, total, size);
        var items = await filtered
            .Include(m => m.PlantaInventario)
            .OrderBy(m => m.Name)
            .ThenBy(m => m.Id)
            .Skip((current - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, total, totalSinFiltro, current);
    }

    public async Task<IReadOnlyList<(int PlantaInventarioId, int Materiales, int Bajo, int Critico)>> SummarizeByPlantaAsync(
        IReadOnlyCollection<int>? allowedPlantaIds,
        CancellationToken cancellationToken = default)
    {
        var query = Scope(_context.Materials.AsNoTracking(), null, allowedPlantaIds);
        var rows = await query
            .GroupBy(m => m.PlantaInventarioId)
            .Select(g => new
            {
                PlantaInventarioId = g.Key,
                Materiales = g.Count(),
                Critico = g.Count(m => m.Stock <= 0),
                Bajo = g.Count(m => m.Stock > 0 && m.MinStock > 0 && m.Stock < m.MinStock)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => (r.PlantaInventarioId, r.Materiales, r.Bajo, r.Critico))
            .ToList();
    }

    private static IQueryable<Material> Scope(
        IQueryable<Material> query,
        int? plantaInventarioId,
        IReadOnlyCollection<int>? allowedPlantaIds)
    {
        if (allowedPlantaIds is not null)
            query = query.Where(m => allowedPlantaIds.Contains(m.PlantaInventarioId));
        if (plantaInventarioId is int id)
            query = query.Where(m => m.PlantaInventarioId == id);
        return query;
    }

    private static IQueryable<Material> ApplyNivel(IQueryable<Material> query, string? nivel)
    {
        if (string.IsNullOrWhiteSpace(nivel))
            return query;

        if (string.Equals(nivel, "Faltantes", StringComparison.OrdinalIgnoreCase))
            return query.Where(m => m.Stock <= 0 || (m.Stock > 0 && m.MinStock > 0 && m.Stock < m.MinStock));

        if (!Enum.TryParse<StockNivel>(nivel, ignoreCase: true, out var parsed))
            return query;

        return parsed switch
        {
            StockNivel.Critico => query.Where(m => m.Stock <= 0),
            StockNivel.Bajo => query.Where(m => m.Stock > 0 && m.MinStock > 0 && m.Stock < m.MinStock),
            _ => query.Where(m => m.Stock > 0 && (m.MinStock <= 0 || m.Stock >= m.MinStock))
        };
    }

    // Busca un material por Id (para editar o ver detalle)
    public Task<Material?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Materials.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    // Agrega un material nuevo al contexto
    public async Task AddAsync(Material material, CancellationToken cancellationToken = default) =>
        await _context.Materials.AddAsync(material, cancellationToken);

    // Marca cambios en un material existente
    public void Update(Material material) => _context.Materials.Update(material);

    public void Remove(Material material) => _context.Materials.Remove(material);
}
