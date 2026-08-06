using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Líneas de material asociadas a una orden de producción
public interface IOrderMaterialRequirementRepository
{
    Task<IReadOnlyList<ProductionOrderMaterialRequirement>> GetByOrderIdAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderMaterialRequirement?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductionOrderMaterialRequirement line, CancellationToken cancellationToken = default);

    void Update(ProductionOrderMaterialRequirement line);

    void Remove(ProductionOrderMaterialRequirement line);

    Task<bool> ExistsAsync(int orderId, int materialId, CancellationToken cancellationToken = default);
}
