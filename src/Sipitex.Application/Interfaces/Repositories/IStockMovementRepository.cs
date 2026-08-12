using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Interfaces.Repositories;

// Ledger de movimientos de inventario (append-only)
public interface IStockMovementRepository
{
    Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<StockMovement> movements, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> QueryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        int? materialId,
        CancellationToken cancellationToken = default);
}
