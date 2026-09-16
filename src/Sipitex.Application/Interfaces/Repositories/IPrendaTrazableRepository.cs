using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IPrendaTrazableRepository
{
    Task AddRangeAsync(IReadOnlyList<PrendaTrazable> items, CancellationToken cancellationToken = default);
    Task<PrendaTrazable?> GetByCodigoAsync(string codigo, CancellationToken cancellationToken = default);
    Task<PrendaTrazable?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrendaTrazable>> ListAsync(
        int? productionOrderId,
        string? query,
        int take = 200,
        CancellationToken cancellationToken = default);
    Task<int> CountByOrderAsync(int productionOrderId, CancellationToken cancellationToken = default);
    Task<string?> GetLastCodigoForPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
