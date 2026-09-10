using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IConsumoMaterialRepository
{
    Task AddAsync(ConsumoMaterial consumo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsumoMaterial>> GetByOrderIdAsync(int productionOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsumoMaterial>> GetByMaterialIdAsync(int materialId, CancellationToken cancellationToken = default);
}
