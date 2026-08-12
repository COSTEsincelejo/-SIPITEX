using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Materiales, stock y solicitudes de bodega
public interface IInventoryService
{
    Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> AddMaterialAsync(CreateMaterialDto dto, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> AdjustStockAsync(AdjustStockDto dto, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateMaterialAsync(UpdateMaterialDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateStatusAsync(UpdateMaterialStatusDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateRequestAsync(CreateMaterialRequestDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> ApproveRequestAsync(int requestId, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> RejectRequestAsync(int requestId, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteMaterialAsync(int materialId, CancellationToken cancellationToken = default);
}
