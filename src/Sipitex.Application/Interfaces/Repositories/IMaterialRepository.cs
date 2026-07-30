using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Catálogo de materiales e inventario
public interface IMaterialRepository
{
    Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Material?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Material material, CancellationToken cancellationToken = default);
    void Update(Material material);
}
