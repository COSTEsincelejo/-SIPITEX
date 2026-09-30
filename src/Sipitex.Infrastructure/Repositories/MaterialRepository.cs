using Microsoft.EntityFrameworkCore; // OrderBy, FirstOrDefaultAsync, AddAsync...
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories; // IMaterialRepository
using Sipitex.Domain.Entities; // Material
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

    // Solo esta lectura ignora el filtro global: el id de planta va explícito en el WHERE.
    public async Task<IReadOnlyList<MaterialPlantaStockDto>> ListStockByPlantaDetalleAsync(
        int plantaInventarioId,
        CancellationToken cancellationToken = default) =>
        await _context.Materials
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.PlantaInventarioId == plantaInventarioId)
            .Select(m => new MaterialPlantaStockDto(
                m.Code,
                m.Name,
                m.Unit,
                m.Stock,
                m.MinStock,
                m.BomItems.Any(),
                m.Id,
                m.Status,
                m.LastEntryDate,
                m.CostoAdquisicion,
                m.CostoPromedioPonderado))
            .ToListAsync(cancellationToken);

    // Busca un material por Id (para editar o ver detalle)
    public Task<Material?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Materials.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    // Solo lo usan las operaciones del inventario de una planta. El WHERE lleva el id de esa planta.
    public Task<Material?> GetByIdInPlantaAsync(int id, int plantaInventarioId, CancellationToken cancellationToken = default) =>
        _context.Materials
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                m => m.Id == id && m.PlantaInventarioId == plantaInventarioId,
                cancellationToken);

    // Agrega un material nuevo al contexto
    public async Task AddAsync(Material material, CancellationToken cancellationToken = default) =>
        await _context.Materials.AddAsync(material, cancellationToken);

    // Marca cambios en un material existente
    public void Update(Material material) => _context.Materials.Update(material);

    public void Remove(Material material) => _context.Materials.Remove(material);
}
