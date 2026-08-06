using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Acceso a plantillas, etapas, movimientos, historial e inventario terminado
public interface IProductionFlowRepository
{
    Task<ProductFlowTemplate?> GetActiveTemplateByProductAsync(string productName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductFlowTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);
    Task AddTemplateAsync(ProductFlowTemplate template, CancellationToken cancellationToken = default);
    void UpdateTemplate(ProductFlowTemplate template);

    Task<IReadOnlyList<ProductionOrderStage>> GetStagesByOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<ProductionOrderStage?> GetStageByIdAsync(int stageId, CancellationToken cancellationToken = default);
    Task AddStageAsync(ProductionOrderStage stage, CancellationToken cancellationToken = default);
    Task AddStagesAsync(IEnumerable<ProductionOrderStage> stages, CancellationToken cancellationToken = default);
    void UpdateStage(ProductionOrderStage stage);
    void RemoveStage(ProductionOrderStage stage);

    Task AddMovementAsync(ProductionOrderStageMovement movement, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionOrderStageMovement>> GetMovementsByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task AddHistoryAsync(ProductionOrderHistoryEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionOrderHistoryEntry>> GetHistoryByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<FinishedGoodStock?> GetFinishedGoodAsync(string productName, CancellationToken cancellationToken = default);
    Task AddFinishedGoodAsync(FinishedGoodStock stock, CancellationToken cancellationToken = default);
    void UpdateFinishedGood(FinishedGoodStock stock);
    Task AddFinishedGoodMovementAsync(FinishedGoodMovement movement, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinishedGoodMovement>> GetFinishedGoodMovementsByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<bool> HasStagePermissionAsync(int userId, string stageName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstructorStagePermission>> GetPermissionsByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task AddPermissionAsync(InstructorStagePermission permission, CancellationToken cancellationToken = default);
    void RemovePermission(InstructorStagePermission permission);
    Task<InstructorStagePermission?> GetPermissionAsync(int userId, string stageName, CancellationToken cancellationToken = default);
}
