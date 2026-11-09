using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IActaMovimientoRepository
{
    Task AddAsync(ActaMovimiento acta, CancellationToken cancellationToken = default);
    Task<ActaMovimiento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActaMovimiento>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<string?> GetLastNumeroAsync(CancellationToken cancellationToken = default);

    /// <summary>Número de acta que ya incluye cada movimiento de stock.</summary>
    Task<IReadOnlyDictionary<int, string>> GetNumerosPorStockMovementAsync(CancellationToken cancellationToken = default);

    /// <summary>Número de acta que ya incluye cada consumo de material.</summary>
    Task<IReadOnlyDictionary<int, string>> GetNumerosPorConsumoAsync(CancellationToken cancellationToken = default);
}
