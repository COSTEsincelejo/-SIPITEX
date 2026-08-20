using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Catálogo de bodegas (Bodega 1 / Bodega 2 seed + altas del admin)
public interface IBodegaRepository
{
    Task<IReadOnlyList<Bodega>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Bodega?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task AddAsync(Bodega bodega, CancellationToken cancellationToken = default);
}
