using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Flujo MES: etapas, movimientos, historial, inventario terminado y permisos
public interface IProductionFlowService
{
    // Copia plantilla (o default) a la orden; idempotente si ya tiene etapas
    Task EnsureStagesForOrderAsync(int orderId, string? actorName = null, CancellationToken cancellationToken = default);

    Task<OrderMesDetailDto?> GetMesDetailAsync(
        int orderId,
        int? actorUserId,
        string? actorRole,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> AddStageAsync(AddOrderStageDto dto, int actorUserId, string actorName, CancellationToken cancellationToken = default);
    Task<ServiceResult> RemoveStageAsync(int stageId, int actorUserId, string actorName, CancellationToken cancellationToken = default);
    Task<ServiceResult> MoveStageAsync(int stageId, int direction, int actorUserId, string actorName, CancellationToken cancellationToken = default);

    Task<ServiceResult> StartStageAsync(int stageId, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default);
    Task<ServiceResult> PauseStageAsync(int stageId, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default);
    Task<ServiceResult> ResumeStageAsync(int stageId, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default);
    Task<ServiceResult> CompleteStageAsync(int stageId, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default);

    Task<ServiceResult> AssignInstructorAsync(AssignStageInstructorDto dto, int actorUserId, string actorName, CancellationToken cancellationToken = default);
    Task<ServiceResult> ProcessUnitsAsync(ProcessStageUnitsDto dto, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default);
    Task<ServiceResult> SendToNextAsync(SendToNextStageDto dto, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default);

    Task<ServiceResult> PartialInventoryInAsync(PartialInventoryInDto dto, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default);

    // Reingreso Bodeguero/Admin desde etapa (material → StockMovement; producto → mismo núcleo que PartialInventoryIn)
    Task<ServiceResult> RegisterStageReentryAsync(
        StageReentryDto dto,
        int actorUserId,
        string actorName,
        string actorRole,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> PartialWithdrawAsync(PartialWithdrawalDto dto, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default);

    Task<ServiceResult> SetStagePermissionAsync(UpsertStagePermissionDto dto, CancellationToken cancellationToken = default);
    Task EnsureDefaultTemplatesAsync(CancellationToken cancellationToken = default);

    Task LogProductionRegisteredAsync(int orderId, int units, string? actorName, CancellationToken cancellationToken = default);
}
