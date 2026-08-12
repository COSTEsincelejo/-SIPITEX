using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IOrderChangeLogRepository
{
    Task AddRangeAsync(IEnumerable<OrderChangeLog> entries, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderChangeLog>> GetByOrderIdAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default);
}
