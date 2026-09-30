using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Materiales, stock y solicitudes de planta de inventario
public interface IInventoryService
{
    Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialDto>> GetMaterialsByPlantaAsync(
        int? plantaInventarioId,
        CancellationToken cancellationToken = default);

    // Stock de una planta concreta, sin el filtro global de bodega activa.
    Task<IReadOnlyList<MaterialPlantaStockDto>> GetStockByPlantaDetalleAsync(
        int plantaInventarioId,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> AddMaterialAsync(CreateMaterialDto dto, int actorUserId, CancellationToken cancellationToken = default);

    // true solo si el material existe y su PlantaInventarioId es el de la ruta (IgnoreQueryFilters + filtro explícito).
    Task<bool> MaterialPerteneceAPlantaAsync(int materialId, int plantaInventarioId, CancellationToken cancellationToken = default);

    Task<ServiceResult> AdjustStockAsync(
        AdjustStockDto dto,
        int actorUserId,
        CancellationToken cancellationToken = default,
        int? plantaInventarioId = null);
    Task<ServiceResult> UpdateMaterialAsync(
        UpdateMaterialDto dto,
        CancellationToken cancellationToken = default,
        int? plantaInventarioId = null);
    Task<ServiceResult> UpdateStatusAsync(
        UpdateMaterialStatusDto dto,
        CancellationToken cancellationToken = default,
        int? plantaInventarioId = null);
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
    Task<ServiceResult> DeleteMaterialAsync(
        int materialId,
        CancellationToken cancellationToken = default,
        int? plantaInventarioId = null);
}
