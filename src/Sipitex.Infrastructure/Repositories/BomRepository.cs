using Microsoft.EntityFrameworkCore; // Include, Where, OrderBy...
using Sipitex.Application.Interfaces.Repositories; // IBomRepository
using Sipitex.Domain.Entities; // BomItem
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Acceso a la tabla BomItems (lista de materiales por prenda)
public class BomRepository : IBomRepository
{
    private readonly SipitexDbContext _context;

    public BomRepository(SipitexDbContext context) => _context = context;

    // Trae los materiales que necesita una prenda (para el cálculo MRP)
    public async Task<IReadOnlyList<BomItem>> GetByProductAsync(string productName, CancellationToken cancellationToken = default) =>
        await _context.BomItems
            .Include(b => b.Material) // Traigo el material completo, no solo el Id
            .Where(b => b.ProductName == productName) // Filtro por prenda: Camisa, Pantalón...
            .ToListAsync(cancellationToken);

    // Lista todo el BOM ordenado por producto y luego por material
    public async Task<IReadOnlyList<BomItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.BomItems
            .Include(b => b.Material)
            .OrderBy(b => b.ProductName)
            .ThenBy(b => b.Material.Name) // Dentro del mismo producto, orden alfabético
            .ToListAsync(cancellationToken);
}
