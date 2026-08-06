using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class ProductionFlowRepository : IProductionFlowRepository
{
    private readonly SipitexDbContext _db;
    public ProductionFlowRepository(SipitexDbContext db) => _db = db;

    public Task<ProductFlowTemplate?> GetActiveTemplateByProductAsync(string productName, CancellationToken cancellationToken = default) =>
        _db.ProductFlowTemplates
            .Include(t => t.Stages)
            .Where(t => t.IsActive && t.ProductName == productName)
            .OrderBy(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductFlowTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default) =>
        await _db.ProductFlowTemplates.Include(t => t.Stages).OrderBy(t => t.ProductName).ToListAsync(cancellationToken);

    public async Task AddTemplateAsync(ProductFlowTemplate template, CancellationToken cancellationToken = default) =>
        await _db.ProductFlowTemplates.AddAsync(template, cancellationToken);

    public void UpdateTemplate(ProductFlowTemplate template) => _db.ProductFlowTemplates.Update(template);

    public async Task<IReadOnlyList<ProductionOrderStage>> GetStagesByOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
        await _db.ProductionOrderStages
            .Include(s => s.InstructorUser)
            .Where(s => s.ProductionOrderId == orderId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<ProductionOrderStage?> GetStageByIdAsync(int stageId, CancellationToken cancellationToken = default) =>
        _db.ProductionOrderStages.Include(s => s.InstructorUser)
            .FirstOrDefaultAsync(s => s.Id == stageId, cancellationToken);

    public async Task AddStageAsync(ProductionOrderStage stage, CancellationToken cancellationToken = default) =>
        await _db.ProductionOrderStages.AddAsync(stage, cancellationToken);

    public async Task AddStagesAsync(IEnumerable<ProductionOrderStage> stages, CancellationToken cancellationToken = default) =>
        await _db.ProductionOrderStages.AddRangeAsync(stages, cancellationToken);

    public void UpdateStage(ProductionOrderStage stage) => _db.ProductionOrderStages.Update(stage);
    public void RemoveStage(ProductionOrderStage stage) => _db.ProductionOrderStages.Remove(stage);

    public async Task AddMovementAsync(ProductionOrderStageMovement movement, CancellationToken cancellationToken = default) =>
        await _db.ProductionOrderStageMovements.AddAsync(movement, cancellationToken);

    public async Task<IReadOnlyList<ProductionOrderStageMovement>> GetMovementsByOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
        await _db.ProductionOrderStageMovements
            .Include(m => m.ActorUser)
            .Include(m => m.FromStage)
            .Include(m => m.ToStage)
            .Where(m => m.ProductionOrderId == orderId)
            .OrderByDescending(m => m.AtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddHistoryAsync(ProductionOrderHistoryEntry entry, CancellationToken cancellationToken = default) =>
        await _db.ProductionOrderHistoryEntries.AddAsync(entry, cancellationToken);

    public async Task<IReadOnlyList<ProductionOrderHistoryEntry>> GetHistoryByOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
        await _db.ProductionOrderHistoryEntries
            .Where(h => h.ProductionOrderId == orderId)
            .OrderByDescending(h => h.AtUtc)
            .ThenByDescending(h => h.Id)
            .ToListAsync(cancellationToken);

    public Task<FinishedGoodStock?> GetFinishedGoodAsync(string productName, CancellationToken cancellationToken = default) =>
        _db.FinishedGoodStocks.FirstOrDefaultAsync(f => f.ProductName == productName, cancellationToken);

    public async Task AddFinishedGoodAsync(FinishedGoodStock stock, CancellationToken cancellationToken = default) =>
        await _db.FinishedGoodStocks.AddAsync(stock, cancellationToken);

    public void UpdateFinishedGood(FinishedGoodStock stock) => _db.FinishedGoodStocks.Update(stock);

    public async Task AddFinishedGoodMovementAsync(FinishedGoodMovement movement, CancellationToken cancellationToken = default) =>
        await _db.FinishedGoodMovements.AddAsync(movement, cancellationToken);

    public async Task<IReadOnlyList<FinishedGoodMovement>> GetFinishedGoodMovementsByOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
        await _db.FinishedGoodMovements
            .Include(m => m.ActorUser)
            .Where(m => m.ProductionOrderId == orderId)
            .OrderByDescending(m => m.AtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasStagePermissionAsync(int userId, string stageName, CancellationToken cancellationToken = default) =>
        _db.InstructorStagePermissions.AnyAsync(
            p => p.UserId == userId && p.StageName == stageName, cancellationToken);

    public async Task<IReadOnlyList<InstructorStagePermission>> GetPermissionsByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await _db.InstructorStagePermissions.Where(p => p.UserId == userId).ToListAsync(cancellationToken);

    public async Task AddPermissionAsync(InstructorStagePermission permission, CancellationToken cancellationToken = default) =>
        await _db.InstructorStagePermissions.AddAsync(permission, cancellationToken);

    public void RemovePermission(InstructorStagePermission permission) =>
        _db.InstructorStagePermissions.Remove(permission);

    public Task<InstructorStagePermission?> GetPermissionAsync(int userId, string stageName, CancellationToken cancellationToken = default) =>
        _db.InstructorStagePermissions.FirstOrDefaultAsync(
            p => p.UserId == userId && p.StageName == stageName, cancellationToken);
}
