using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Consulta del historial de movimientos de inventario
public interface IStockMovementService
{
    Task<IReadOnlyList<StockMovementDto>> GetHistoryAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        int? materialId,
        CancellationToken cancellationToken = default);
}
