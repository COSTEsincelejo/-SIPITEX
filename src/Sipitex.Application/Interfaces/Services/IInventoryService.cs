using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IInventoryService
{
    Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> AddMaterialAsync(CreateMaterialDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> AdjustStockAsync(AdjustStockDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateRequestAsync(CreateMaterialRequestDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> ApproveRequestAsync(int requestId, CancellationToken cancellationToken = default);
}
