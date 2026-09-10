using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IGrupoConfeccionRepository
{
    Task AddAsync(GrupoConfeccion grupo, CancellationToken cancellationToken = default);
    void Update(GrupoConfeccion grupo);
    Task<GrupoConfeccion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrupoConfeccion>> GetByOrderIdAsync(int productionOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrupoConfeccion>> GetAllAsync(CancellationToken cancellationToken = default);
}
