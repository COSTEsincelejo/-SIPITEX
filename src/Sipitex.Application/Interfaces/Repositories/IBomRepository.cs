using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Bill of Materials: qué materiales lleva cada producto
public interface IBomRepository
{
    Task<IReadOnlyList<BomItem>> GetByProductAsync(string productName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BomItem>> GetAllAsync(CancellationToken cancellationToken = default);
}
