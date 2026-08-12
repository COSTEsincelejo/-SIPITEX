using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

public class StockMovementService : IStockMovementService
{
    private readonly IStockMovementRepository _movements;

    public StockMovementService(IStockMovementRepository movements) => _movements = movements;

    public async Task<IReadOnlyList<StockMovementDto>> GetHistoryAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        int? materialId,
        CancellationToken cancellationToken = default)
    {
        DateTime? fromUtc = fromDate is DateOnly f
            ? DateTime.SpecifyKind(f.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;
        DateTime? toUtc = toDate is DateOnly t
            ? DateTime.SpecifyKind(t.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Utc)
            : null;

        var rows = await _movements.QueryAsync(fromUtc, toUtc, materialId, cancellationToken);
        return rows.Select(m => new StockMovementDto(
            m.Id,
            m.FechaUtc,
            m.Usuario?.Nombre ?? $"#{m.UsuarioId}",
            m.UsuarioId,
            m.TipoMovimiento,
            m.MaterialId,
            m.Material?.Name ?? $"#{m.MaterialId}",
            m.Cantidad,
            m.StockResultante,
            m.Referencia)).ToList();
    }
}
