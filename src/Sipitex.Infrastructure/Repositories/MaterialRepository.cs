using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class MaterialRepository : IMaterialRepository
{
    private readonly SipitexDbContext _context;

    public MaterialRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Materials.OrderBy(m => m.Name).ToListAsync(cancellationToken);

    public Task<Material?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Materials.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<Material?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var key = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(key))
            return Task.FromResult<Material?>(null);

        return _context.Materials.FirstOrDefaultAsync(m => m.Name.ToLower() == key, cancellationToken);
    }

    public async Task AddAsync(Material material, CancellationToken cancellationToken = default) =>
        await _context.Materials.AddAsync(material, cancellationToken);

    public void Update(Material material) => _context.Materials.Update(material);
}
