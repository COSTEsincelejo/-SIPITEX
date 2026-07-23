using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IProductionSessionRepository
{
    Task<IReadOnlyList<ProductionSession>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionSession>> GetInDateRangeAsync(DateTime fromInclusive, DateTime toExclusive, CancellationToken cancellationToken = default);
    Task AddAsync(ProductionSession session, CancellationToken cancellationToken = default);
}
