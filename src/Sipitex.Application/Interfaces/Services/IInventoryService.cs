using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;

namespace Sipitex.Application.Interfaces.Services;

// Materiales, stock y solicitudes de planta de inventario
public interface IInventoryService
{
    Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialDto>> GetMaterialsByPlantaAsync(
        int? plantaInventarioId,
        CancellationToken cancellationToken = default);
    Task<MaterialPageDto> GetMaterialsPageAsync(
        int? plantaInventarioId,
        string? nombre,
        string? nivel,
        int? page,
        int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlantaStockConteoDto>> SummarizeStockAsync(
        CancellationToken cancellationToken = default);
    Task<ServiceResult> AddMaterialAsync(CreateMaterialDto dto, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> AdjustStockAsync(AdjustStockDto dto, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateMaterialAsync(UpdateMaterialDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateStatusAsync(UpdateMaterialStatusDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialRequestDto>> GetRequestsAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateRequestAsync(
        CreateMaterialRequestDto dto,
        int? solicitanteId = null,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> ApproveRequestAsync(int requestId, int actorUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult> RejectRequestAsync(int requestId, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteMaterialAsync(int materialId, CancellationToken cancellationToken = default);
}
