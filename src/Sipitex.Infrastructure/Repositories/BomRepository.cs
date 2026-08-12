using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

// Acceso a BomProducts / BomItems
public class BomRepository : IBomRepository
{
    private readonly SipitexDbContext _context;

    public BomRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<BomItem>> GetByProductAsync(string productName, CancellationToken cancellationToken = default) =>
        await _context.BomItems
            .Include(b => b.Material)
            .Include(b => b.BomProduct)
            .Where(b => b.ProductName == productName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BomItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.BomItems
            .Include(b => b.Material)
            .Include(b => b.BomProduct)
            .OrderBy(b => b.ProductName)
            .ThenBy(b => b.Material.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BomProduct>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        await _context.BomProducts
            .Include(p => p.Items)
                .ThenInclude(i => i.Material)
            .Include(p => p.Instructors)
                .ThenInclude(i => i.User)
            .Include(p => p.Tallas)
            .Include(p => p.Piezas)
            .Include(p => p.Medidas)
                .ThenInclude(m => m.Valores)
            .OrderBy(p => p.ProductName)
            .ToListAsync(cancellationToken);

    public Task<BomProduct?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.BomProducts
            .Include(p => p.Items)
                .ThenInclude(i => i.Material)
            .Include(p => p.Instructors)
                .ThenInclude(i => i.User)
            .Include(p => p.Tallas)
            .Include(p => p.Piezas)
            .Include(p => p.Medidas)
                .ThenInclude(m => m.Valores)
                    .ThenInclude(v => v.Talla)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<BomProduct?> GetProductByNameAsync(string productName, CancellationToken cancellationToken = default) =>
        _context.BomProducts
            .Include(p => p.Items)
                .ThenInclude(i => i.Material)
            .Include(p => p.Tallas)
            .Include(p => p.Piezas)
            .Include(p => p.Medidas)
                .ThenInclude(m => m.Valores)
            .FirstOrDefaultAsync(p => p.ProductName == productName, cancellationToken);

    public async Task AddProductAsync(BomProduct product, CancellationToken cancellationToken = default) =>
        await _context.BomProducts.AddAsync(product, cancellationToken);

    public void UpdateProduct(BomProduct product) => _context.BomProducts.Update(product);

    public void RemoveProduct(BomProduct product) => _context.BomProducts.Remove(product);

    public async Task AddItemAsync(BomItem item, CancellationToken cancellationToken = default) =>
        await _context.BomItems.AddAsync(item, cancellationToken);

    public void UpdateItem(BomItem item) => _context.BomItems.Update(item);

    public void RemoveItem(BomItem item) => _context.BomItems.Remove(item);

    public void RemoveTalla(BomProductTalla talla) => _context.BomProductTallas.Remove(talla);

    public void RemovePieza(BomProductPieza pieza) => _context.BomProductPiezas.Remove(pieza);

    public void RemoveMedida(BomProductMedida medida) => _context.BomProductMedidas.Remove(medida);

    public async Task<IReadOnlyList<string>> GetProductNamesUsingMaterialAsync(int materialId, CancellationToken cancellationToken = default) =>
        await _context.BomItems
            .Where(b => b.MaterialId == materialId)
            .Select(b => b.ProductName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);
}
