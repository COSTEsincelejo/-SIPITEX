using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Catálogo de bodegas (Bodega 1 / Bodega 2); solo lectura en Fase 2
public interface IBodegaRepository
{
    Task<IReadOnlyList<Bodega>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Bodega?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
