using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IBomRepository
{
    Task<IReadOnlyList<BomItem>> GetByProductAsync(string productName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctProductNamesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(BomItem item, CancellationToken cancellationToken = default);
}
