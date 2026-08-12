using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// CRUD de fichas técnicas (productos BOM) y asignación a instructores
public interface IBomCatalogService
{
    // assignedInstructorUserId: si se indica, solo fichas técnicas asignadas a ese instructor
    Task<IReadOnlyList<BomProductListItemDto>> GetProductsAsync(
        int? assignedInstructorUserId = null,
        CancellationToken cancellationToken = default);

    Task<BomProductDetailDto?> GetProductAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetOrderEligibleProductNamesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(UpsertBomProductDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(int id, UpsertBomProductDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult> AssignInstructorAsync(
        int bomProductId,
        int instructorUserId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> RemoveInstructorAsync(
        int bomProductId,
        int instructorUserId,
        CancellationToken cancellationToken = default);

    // Instructores activos disponibles para asignar a fichas técnicas
    Task<IReadOnlyList<InstructorOptionDto>> GetAssignableInstructorsAsync(CancellationToken cancellationToken = default);
}
