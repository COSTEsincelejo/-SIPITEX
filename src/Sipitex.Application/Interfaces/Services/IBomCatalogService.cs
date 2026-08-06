using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// CRUD de fichas técnicas (productos BOM)
public interface IBomCatalogService
{
    Task<IReadOnlyList<BomProductListItemDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<BomProductDetailDto?> GetProductAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetOrderEligibleProductNamesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(UpsertBomProductDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(int id, UpsertBomProductDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
