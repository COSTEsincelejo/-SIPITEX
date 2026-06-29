using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class BomRepository : IBomRepository
{
    private readonly SipitexDbContext _context;

    public BomRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<BomItem>> GetByProductAsync(string productName, CancellationToken cancellationToken = default) =>
        await _context.BomItems
            .Include(b => b.Material)
            .Where(b => b.ProductName == productName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BomItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.BomItems
            .Include(b => b.Material)
            .OrderBy(b => b.ProductName)
            .ThenBy(b => b.Material.Name)
            .ToListAsync(cancellationToken);
}
