using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IActaMovimientoRepository
{
    Task AddAsync(ActaMovimiento acta, CancellationToken cancellationToken = default);
    Task<ActaMovimiento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActaMovimiento>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<string?> GetLastNumeroAsync(CancellationToken cancellationToken = default);
}
