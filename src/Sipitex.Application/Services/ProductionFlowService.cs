using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Flujo MES aditivo: no altera consumo BOM ni materiales de bodega
public class ProductionFlowService : IProductionFlowService
{
    public static readonly string[] DefaultStageNames =
        ["Trazo", "Corte", "Confección", "Control de Calidad", "Terminado"];

    private readonly IProductionOrderRepository _orders;
    private readonly IProductionFlowRepository _flow;
    private readonly IOrderMaterialRequirementRepository _materials;
    private readonly IProductionOrderBomSnapshotRepository _snapshots;
    private readonly IBomRepository _boms;
    private readonly IUserRepository _users;
    private readonly IMaterialRepository _materialRepository;
    private readonly IStockMovementRepository _stockMovements;
    private readonly IUnitOfWork _uow;

    public ProductionFlowService(
        IProductionOrderRepository orders,
        IProductionFlowRepository flow,
        IOrderMaterialRequirementRepository materials,
        IProductionOrderBomSnapshotRepository snapshots,
        IBomRepository boms,
        IUserRepository users,
        IMaterialRepository materialRepository,
        IStockMovementRepository stockMovements,
        IUnitOfWork uow)
    {
        _orders = orders;
        _flow = flow;
        _materials = materials;
        _snapshots = snapshots;
        _boms = boms;
        _users = users;
        _materialRepository = materialRepository;
        _stockMovements = stockMovements;
        _uow = uow;
    }

    public async Task EnsureDefaultTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _flow.GetAllTemplatesAsync(cancellationToken);
        if (existing.Count > 0) return;

        foreach (var product in new[] { "Camisa", "Pantalón", "*" })
        {
            var template = new ProductFlowTemplate
            {
                ProductName = product,
                Name = product == "*" ? "Flujo genérico SIPITEX" : $"Flujo {product}",
                IsActive = true,
                Stages = DefaultStageNames.Select((n, i) => new ProductFlowStageTemplate
                {
                    Name = n,
                    SortOrder = i + 1,
                    IsOptional = false
                }).ToList()
            };
            await _flow.AddTemplateAsync(template, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureStagesForOrderAsync(
        int orderId,
        string? actorName = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultTemplatesAsync(cancellationToken);

        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.Status == OrderStatus.Cancelada) return;

        var stages = await _flow.GetStagesByOrderAsync(orderId, cancellationToken);
        if (stages.Count > 0) return;

        var template = await _flow.GetActiveTemplateByProductAsync(order.ProductName, cancellationToken)
                       ?? await _flow.GetActiveTemplateByProductAsync("*", cancellationToken);

        var defs = template?.Stages.OrderBy(s => s.SortOrder).ToList()
                   ?? DefaultStageNames.Select((n, i) => new ProductFlowStageTemplate
                   {
                       Name = n,
                       SortOrder = i + 1
                   }).ToList();

        var created = defs.Select(d => new ProductionOrderStage
        {
            ProductionOrderId = orderId,
            Name = d.Name,
            SortOrder = d.SortOrder,
            IsOptional = d.IsOptional,
            Status = ProductionStageStatus.Pendiente,
            QuantityReceived = d.SortOrder == 1 ? order.TotalQuantity : 0
        }).ToList();

        await _flow.AddStagesAsync(created, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Recargar para Ids
        stages = await _flow.GetStagesByOrderAsync(orderId, cancellationToken);
        var first = stages.OrderBy(s => s.SortOrder).FirstOrDefault();
        if (first is not null)
        {
            order.CurrentStageId = first.Id;
            _orders.Update(order);
        }

        await _flow.AddHistoryAsync(new ProductionOrderHistoryEntry
        {
            ProductionOrderId = orderId,
            EventType = ProductionHistoryEventType.OrderCreated,
            Message = $"Flujo inicializado con {stages.Count} etapas.",
            ActorUserName = actorName
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderMesDetailDto?> GetMesDetailAsync(
        int orderId,
        int? actorUserId,
        string? actorRole,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null) return null;

        await EnsureStagesForOrderAsync(orderId, cancellationToken: cancellationToken);
        order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null) return null;

        var stages = await _flow.GetStagesByOrderAsync(orderId, cancellationToken);
        var history = await _flow.GetHistoryByOrderAsync(orderId, cancellationToken);
        var movements = await _flow.GetMovementsByOrderAsync(orderId, cancellationToken);
        var fgMoves = await _flow.GetFinishedGoodMovementsByOrderAsync(orderId, cancellationToken);
        var matLines = await _materials.GetByOrderIdAsync(orderId, cancellationToken);
        var fg = await _flow.GetFinishedGoodAsync(order.ProductName, cancellationToken);

        var qtyPct = order.TotalQuantity > 0
            ? Math.Min(100, (int)Math.Round(order.ProducedQuantity * 100m / order.TotalQuantity))
            : 0;
        var flowPct = ComputeFlowPercent(stages);
        var combined = (qtyPct + flowPct) / 2;

        var currentName = stages.FirstOrDefault(s => s.Id == order.CurrentStageId)?.Name
                          ?? stages.FirstOrDefault(s => s.Status != ProductionStageStatus.Finalizado)?.Name;

        var canManage = string.Equals(actorRole, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase)
                        && order.Status == OrderStatus.EnProceso;

        var mrp = await BuildMrpHintAsync(order, cancellationToken);

        return new OrderMesDetailDto(
            order.Id,
            order.OrderNumber,
            order.ProductName,
            order.ClientName,
            order.Status,
            order.MaterialsStatus,
            order.TotalQuantity,
            order.ProducedQuantity,
            qtyPct,
            flowPct,
            combined,
            currentName,
            order.Deadline,
            mrp,
            fg?.Stock ?? 0,
            canManage,
            stages.Select(s => new OrderStageDto(
                s.Id, s.Name, s.SortOrder, s.IsOptional, s.Status,
                s.InstructorUserId, s.InstructorUser?.Nombre,
                s.StartedAtUtc, s.CompletedAtUtc, s.Observations,
                s.QuantityReceived, s.QuantityProcessed, s.QuantitySent, s.QuantityWithdrawn,
                s.QuantityAvailable,
                s.Id == order.CurrentStageId)).ToList(),
            matLines.Select(MapMaterialLine).ToList(),
            history.Select(h => new OrderHistoryDto(
                h.Id, h.AtUtc, h.EventType, h.Message, h.ActorUserName, h.StageName, h.Quantity)).ToList(),
            movements.Select(m => new OrderStageMovementDto(
                m.Id, m.MovementType, m.Quantity, m.AtUtc,
                m.ActorUser?.Nombre ?? $"#{m.ActorUserId}",
                m.FromStage?.Name, m.ToStage?.Name, m.Motive, m.Observations)).ToList(),
            fgMoves.Select(f => new FinishedGoodMovementDto(
                f.Id, f.ProductName, f.Quantity, f.AtUtc,
                f.ActorUser?.Nombre ?? $"#{f.ActorUserId}", f.Observations)).ToList());
    }

    public async Task<ServiceResult> AddStageAsync(
        AddOrderStageDto dto, int actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return ServiceResult.Fail("Nombre de etapa obligatorio.");

        var order = await _orders.GetByIdAsync(dto.OrderId, cancellationToken);
        if (order is null) return ServiceResult.Fail("Orden no encontrada.");
        var closed = RejectIfOrderClosed(order);
        if (closed is not null) return closed;
        var stages = await _flow.GetStagesByOrderAsync(dto.OrderId, cancellationToken);
        var nextOrder = stages.Count == 0 ? 1 : stages.Max(s => s.SortOrder) + 1;

        await _flow.AddStageAsync(new ProductionOrderStage
        {
            ProductionOrderId = dto.OrderId,
            Name = dto.Name.Trim(),
            SortOrder = nextOrder,
            IsOptional = dto.IsOptional,
            Status = ProductionStageStatus.Pendiente
        }, cancellationToken);

        await AddHistory(dto.OrderId, ProductionHistoryEventType.StageAdded,
            $"Etapa «{dto.Name.Trim()}» agregada.", actorUserId, actorName, null, null, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Etapa agregada.");
    }

    public async Task<ServiceResult> RemoveStageAsync(
        int stageId, int actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var closed = await EnsureOrderNotClosedByStageAsync(stageId, cancellationToken);
        if (closed is not null) return closed;

        var stage = await _flow.GetStageByIdAsync(stageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");
        if (stage.QuantitySent > 0 || stage.QuantityProcessed > 0 || stage.QuantityWithdrawn > 0)
            return ServiceResult.Fail("No se puede eliminar una etapa con movimientos registrados.");
        var order = await _orders.GetByIdAsync(stage.ProductionOrderId, cancellationToken);
        var name = stage.Name;
        _flow.RemoveStage(stage);

        if (order?.CurrentStageId == stageId)
        {
            order.CurrentStageId = null;
            _orders.Update(order);
        }

        await AddHistory(stage.ProductionOrderId, ProductionHistoryEventType.StageRemoved,
            $"Etapa «{name}» eliminada.", actorUserId, actorName, null, null, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Etapa eliminada.");
    }

    public async Task<ServiceResult> MoveStageAsync(
        int stageId, int direction, int actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var closed = await EnsureOrderNotClosedByStageAsync(stageId, cancellationToken);
        if (closed is not null) return closed;

        var stage = await _flow.GetStageByIdAsync(stageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");

        var stages = (await _flow.GetStagesByOrderAsync(stage.ProductionOrderId, cancellationToken))
            .OrderBy(s => s.SortOrder).ToList();
        var idx = stages.FindIndex(s => s.Id == stageId);
        var swapIdx = idx + (direction < 0 ? -1 : 1);
        if (idx < 0 || swapIdx < 0 || swapIdx >= stages.Count)
            return ServiceResult.Fail("No se puede reordenar en esa dirección.");

        (stages[idx].SortOrder, stages[swapIdx].SortOrder) = (stages[swapIdx].SortOrder, stages[idx].SortOrder);
        _flow.UpdateStage(stages[idx]);
        _flow.UpdateStage(stages[swapIdx]);

        await AddHistory(stage.ProductionOrderId, ProductionHistoryEventType.StageReordered,
            $"Etapas reordenadas: «{stages[idx].Name}» ↔ «{stages[swapIdx].Name}».",
            actorUserId, actorName, null, null, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Orden de etapas actualizado.");
    }

    public Task<ServiceResult> StartStageAsync(int stageId, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default) =>
        ChangeStageStatusAsync(stageId, ProductionStageStatus.EnProceso, actorUserId, actorName, actorRole,
            ProductionHistoryEventType.StageStarted, "iniciada", cancellationToken);

    public Task<ServiceResult> PauseStageAsync(int stageId, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default) =>
        ChangeStageStatusAsync(stageId, ProductionStageStatus.Pausado, actorUserId, actorName, actorRole,
            ProductionHistoryEventType.StagePaused, "pausada", cancellationToken);

    public Task<ServiceResult> ResumeStageAsync(int stageId, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default) =>
        ChangeStageStatusAsync(stageId, ProductionStageStatus.EnProceso, actorUserId, actorName, actorRole,
            ProductionHistoryEventType.StageResumed, "reanudada", cancellationToken);

    public async Task<ServiceResult> CompleteStageAsync(
        int stageId, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default)
    {
        var gate = await EnsureCanActOnStageAsync(stageId, actorUserId, actorRole, cancellationToken);
        if (gate is not null) return gate;

        var stage = await _flow.GetStageByIdAsync(stageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");

        stage.Status = ProductionStageStatus.Finalizado;
        stage.CompletedAtUtc = DateTime.UtcNow;
        _flow.UpdateStage(stage);

        var order = await _orders.GetByIdAsync(stage.ProductionOrderId, cancellationToken);
        var stages = await _flow.GetStagesByOrderAsync(stage.ProductionOrderId, cancellationToken);
        var next = stages.Where(s => s.SortOrder > stage.SortOrder)
            .OrderBy(s => s.SortOrder).FirstOrDefault();

        if (order is not null)
        {
            order.CurrentStageId = next?.Id ?? stage.Id;
            _orders.Update(order);
        }

        await AddHistory(stage.ProductionOrderId, ProductionHistoryEventType.StageCompleted,
            $"Etapa «{stage.Name}» finalizada." + (next is null ? "" : $" Siguiente: «{next.Name}»."),
            actorUserId, actorName, stage.Id, stage.Name, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(next is null
            ? "Etapa finalizada. No hay más etapas en el flujo."
            : $"Etapa finalizada. Flujo en «{next.Name}».");
    }

    public async Task<ServiceResult> AssignInstructorAsync(
        AssignStageInstructorDto dto, int actorUserId, string actorName, CancellationToken cancellationToken = default)
    {
        var stage = await _flow.GetStageByIdAsync(dto.StageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");
        var closed = await EnsureOrderNotClosedByStageAsync(dto.StageId, cancellationToken);
        if (closed is not null) return closed;
        if (dto.InstructorUserId is int uid)
        {
            var user = await _users.GetByIdAsync(uid, cancellationToken);
            if (user is null || user.Rol != UserRoles.Instructor)
                return ServiceResult.Fail("Seleccione un instructor válido.");
            stage.InstructorUserId = uid;
        }
        else
        {
            stage.InstructorUserId = null;
        }

        _flow.UpdateStage(stage);
        await AddHistory(stage.ProductionOrderId, ProductionHistoryEventType.InstructorAssigned,
            dto.InstructorUserId is null
                ? $"Instructor desasignado de «{stage.Name}»."
                : $"Instructor asignado a «{stage.Name}».",
            actorUserId, actorName, stage.Id, stage.Name, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Instructor actualizado.");
    }

    public async Task<ServiceResult> ProcessUnitsAsync(
        ProcessStageUnitsDto dto, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default)
    {
        if (dto.Quantity <= 0) return ServiceResult.Fail("Cantidad inválida.");
        var gate = await EnsureCanActOnStageAsync(dto.StageId, actorUserId, actorRole, cancellationToken);
        if (gate is not null) return gate;

        var stage = await _flow.GetStageByIdAsync(dto.StageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");
        if (dto.Quantity > stage.QuantityAvailable)
            return ServiceResult.Fail($"Solo hay {stage.QuantityAvailable} unidades disponibles en la etapa.");

        stage.QuantityProcessed += dto.Quantity;
        if (stage.Status == ProductionStageStatus.Pendiente)
        {
            stage.Status = ProductionStageStatus.EnProceso;
            stage.StartedAtUtc ??= DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(dto.Observations))
            stage.Observations = dto.Observations.Trim();

        _flow.UpdateStage(stage);
        await AddHistory(stage.ProductionOrderId, ProductionHistoryEventType.Note,
            $"Procesadas {dto.Quantity} uds en «{stage.Name}».",
            actorUserId, actorName, stage.Id, stage.Name, cancellationToken, dto.Quantity);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Se registraron {dto.Quantity} unidades procesadas.");
    }

    public async Task<ServiceResult> SendToNextAsync(
        SendToNextStageDto dto, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default)
    {
        if (dto.Quantity <= 0) return ServiceResult.Fail("Cantidad inválida.");
        var gate = await EnsureCanActOnStageAsync(dto.FromStageId, actorUserId, actorRole, cancellationToken);
        if (gate is not null) return gate;

        var from = await _flow.GetStageByIdAsync(dto.FromStageId, cancellationToken);
        if (from is null) return ServiceResult.Fail("Etapa origen no encontrada.");
        if (dto.Quantity > from.QuantityAvailable)
            return ServiceResult.Fail($"Solo hay {from.QuantityAvailable} unidades disponibles para enviar.");

        var stages = await _flow.GetStagesByOrderAsync(from.ProductionOrderId, cancellationToken);
        var next = stages.Where(s => s.SortOrder > from.SortOrder).OrderBy(s => s.SortOrder).FirstOrDefault();
        if (next is null)
            return ServiceResult.Fail("No hay etapa siguiente. Use ingreso a inventario o finalice el flujo.");

        from.QuantitySent += dto.Quantity;
        next.QuantityReceived += dto.Quantity;
        if (next.Status == ProductionStageStatus.Pendiente)
        {
            next.Status = ProductionStageStatus.EnProceso;
            next.StartedAtUtc ??= DateTime.UtcNow;
        }

        _flow.UpdateStage(from);
        _flow.UpdateStage(next);

        var order = await _orders.GetByIdAsync(from.ProductionOrderId, cancellationToken);
        if (order is not null)
        {
            order.CurrentStageId = next.Id;
            _orders.Update(order);
        }

        await _flow.AddMovementAsync(new ProductionOrderStageMovement
        {
            ProductionOrderId = from.ProductionOrderId,
            FromStageId = from.Id,
            ToStageId = next.Id,
            MovementType = "Send",
            Quantity = dto.Quantity,
            ActorUserId = actorUserId,
            Observations = dto.Observations
        }, cancellationToken);

        await AddHistory(from.ProductionOrderId, ProductionHistoryEventType.StageSent,
            $"Enviadas {dto.Quantity} uds de «{from.Name}» a «{next.Name}».",
            actorUserId, actorName, from.Id, from.Name, cancellationToken, dto.Quantity);
        await AddHistory(from.ProductionOrderId, ProductionHistoryEventType.StageReceived,
            $"Recibidas {dto.Quantity} uds en «{next.Name}».",
            actorUserId, actorName, next.Id, next.Name, cancellationToken, dto.Quantity);

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Enviadas {dto.Quantity} unidades a «{next.Name}».");
    }

    public async Task<ServiceResult> PartialInventoryInAsync(
        PartialInventoryInDto dto, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default)
    {
        if (dto.Quantity <= 0) return ServiceResult.Fail("Cantidad inválida.");
        var gate = await EnsureCanActOnStageAsync(dto.StageId, actorUserId, actorRole, cancellationToken);
        if (gate is not null) return gate;

        var result = await ApplyFinishedGoodInventoryInAsync(
            dto.OrderId, dto.StageId, dto.Quantity, dto.Observations,
            actorUserId, actorName, cancellationToken);
        if (!result.Success) return result;

        await _uow.SaveChangesAsync(cancellationToken);
        return result;
    }

    // Gap #14: reingreso Bodeguero/Admin desde Trazo…Terminado (material → ledger; producto → PartialInventoryIn)
    public async Task<ServiceResult> RegisterStageReentryAsync(
        StageReentryDto dto,
        int actorUserId,
        string actorName,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        if (!IsWarehouseOrAdmin(actorRole))
            return ServiceResult.Fail("Solo Bodeguero o Administrador pueden registrar reingresos desde etapas.");

        if (dto.Quantity <= 0) return ServiceResult.Fail("Cantidad inválida.");

        var orderGate = await _orders.GetByIdAsync(dto.OrderId, cancellationToken);
        if (orderGate is null) return ServiceResult.Fail("Orden no encontrada.");
        var closed = RejectIfOrderClosed(orderGate);
        if (closed is not null) return closed;

        // Bodeguero/Admin: no exige permiso de instructor por etapa (gap #14)
        var stage = await _flow.GetStageByIdAsync(dto.StageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");
        if (stage.ProductionOrderId != dto.OrderId)
            return ServiceResult.Fail("La etapa no pertenece a la orden indicada.");
        if (!DefaultStageNames.Contains(stage.Name, StringComparer.OrdinalIgnoreCase))
            return ServiceResult.Fail("La etapa de origen debe ser Trazo, Corte, Confección, Control de Calidad o Terminado.");

        if (dto.MaterialId is int materialId)
        {
            var materialResult = await ApplyMaterialStageReentryAsync(
                dto.OrderId, stage, materialId, dto.Quantity, dto.Observations,
                actorUserId, actorName, cancellationToken);
            if (!materialResult.Success) return materialResult;
            await _uow.SaveChangesAsync(cancellationToken);
            return materialResult;
        }

        var fgResult = await ApplyFinishedGoodInventoryInAsync(
            dto.OrderId, dto.StageId, dto.Quantity, dto.Observations,
            actorUserId, actorName, cancellationToken);
        if (!fgResult.Success) return fgResult;
        await _uow.SaveChangesAsync(cancellationToken);
        return fgResult;
    }

    public async Task<ServiceResult> PartialWithdrawAsync(
        PartialWithdrawalDto dto, int actorUserId, string actorName, string actorRole, CancellationToken cancellationToken = default)
    {
        if (dto.Quantity <= 0) return ServiceResult.Fail("Cantidad inválida.");
        if (string.IsNullOrWhiteSpace(dto.Motive))
            return ServiceResult.Fail("El motivo es obligatorio.");

        var gate = await EnsureCanActOnStageAsync(dto.StageId, actorUserId, actorRole, cancellationToken);
        if (gate is not null) return gate;

        var stage = await _flow.GetStageByIdAsync(dto.StageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");
        if (dto.Quantity > stage.QuantityAvailable)
            return ServiceResult.Fail($"Solo hay {stage.QuantityAvailable} unidades disponibles.");

        stage.QuantityWithdrawn += dto.Quantity;
        _flow.UpdateStage(stage);

        await _flow.AddMovementAsync(new ProductionOrderStageMovement
        {
            ProductionOrderId = stage.ProductionOrderId,
            FromStageId = stage.Id,
            MovementType = "Withdraw",
            Quantity = dto.Quantity,
            ActorUserId = actorUserId,
            AuthorizedByUserId = dto.AuthorizedByUserId,
            Motive = dto.Motive.Trim(),
            Observations = dto.Observations
        }, cancellationToken);

        await AddHistory(stage.ProductionOrderId, ProductionHistoryEventType.PartialWithdrawal,
            $"Salida parcial de «{stage.Name}»: {dto.Quantity} uds. Motivo: {dto.Motive.Trim()}.",
            actorUserId, actorName, stage.Id, stage.Name, cancellationToken, dto.Quantity);

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Salida parcial registrada y auditada.");
    }

    public async Task<ServiceResult> SetStagePermissionAsync(
        UpsertStagePermissionDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.StageName))
            return ServiceResult.Fail("Nombre de etapa obligatorio.");

        var existing = await _flow.GetPermissionAsync(dto.UserId, dto.StageName.Trim(), cancellationToken);
        if (dto.Allowed)
        {
            if (existing is null)
            {
                await _flow.AddPermissionAsync(new InstructorStagePermission
                {
                    UserId = dto.UserId,
                    StageName = dto.StageName.Trim()
                }, cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);
            }
            return ServiceResult.Ok("Permiso otorgado.");
        }

        if (existing is not null)
        {
            _flow.RemovePermission(existing);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Ok("Permiso revocado.");
    }

    public async Task LogProductionRegisteredAsync(
        int orderId, int units, string? actorName, CancellationToken cancellationToken = default)
    {
        await AddHistory(orderId, ProductionHistoryEventType.ProductionRegistered,
            $"Producción registrada: +{units} unidades.",
            null, actorName, null, null, cancellationToken, units);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task<ServiceResult> ChangeStageStatusAsync(
        int stageId,
        ProductionStageStatus newStatus,
        int actorUserId,
        string actorName,
        string actorRole,
        ProductionHistoryEventType eventType,
        string verb,
        CancellationToken cancellationToken)
    {
        var gate = await EnsureCanActOnStageAsync(stageId, actorUserId, actorRole, cancellationToken);
        if (gate is not null) return gate;

        var stage = await _flow.GetStageByIdAsync(stageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");

        stage.Status = newStatus;
        if (newStatus == ProductionStageStatus.EnProceso)
            stage.StartedAtUtc ??= DateTime.UtcNow;
        _flow.UpdateStage(stage);

        var order = await _orders.GetByIdAsync(stage.ProductionOrderId, cancellationToken);
        if (order is not null && newStatus == ProductionStageStatus.EnProceso)
        {
            order.CurrentStageId = stage.Id;
            _orders.Update(order);
        }

        await AddHistory(stage.ProductionOrderId, eventType,
            $"Etapa «{stage.Name}» {verb}.",
            actorUserId, actorName, stage.Id, stage.Name, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Etapa {verb}.");
    }

    private async Task<ServiceResult?> EnsureOrderNotClosedByStageAsync(
        int stageId, CancellationToken cancellationToken)
    {
        var stage = await _flow.GetStageByIdAsync(stageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");
        var order = await _orders.GetByIdAsync(stage.ProductionOrderId, cancellationToken);
        if (order is null) return ServiceResult.Fail("Orden no encontrada.");
        return RejectIfOrderClosed(order);
    }

    private static ServiceResult? RejectIfOrderClosed(ProductionOrder order)
    {
        if (order.Status == OrderStatus.Pendiente)
            return ServiceResult.Fail(
                "La orden está pendiente de aprobación del Administrador y no admite operaciones MES.");
        if (order.Status == OrderStatus.Cancelada)
            return ServiceResult.Fail("La orden está cancelada y no admite más operaciones.");
        if (order.Status == OrderStatus.Finalizada)
            return ServiceResult.Fail("La orden está finalizada y no admite más operaciones.");
        return null;
    }

    private async Task<ServiceResult?> EnsureCanActOnStageAsync(
        int stageId, int actorUserId, string actorRole, CancellationToken cancellationToken)
    {
        var closed = await EnsureOrderNotClosedByStageAsync(stageId, cancellationToken);
        if (closed is not null) return closed;

        if (string.Equals(actorRole, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return null;

        var stage = await _flow.GetStageByIdAsync(stageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");

        if (stage.InstructorUserId is int assigned && assigned == actorUserId)
            return null;

        if (await _flow.HasStagePermissionAsync(actorUserId, stage.Name, cancellationToken))
            return null;

        return ServiceResult.Fail($"Sin permiso para operar la etapa «{stage.Name}».");
    }

    private static bool IsWarehouseOrAdmin(string actorRole) =>
        string.Equals(actorRole, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase)
        || string.Equals(actorRole, UserRoles.Bodeguero, StringComparison.OrdinalIgnoreCase);

    // Núcleo compartido con PartialInventoryIn (producto terminado) — sin SaveChanges
    private async Task<ServiceResult> ApplyFinishedGoodInventoryInAsync(
        int orderId,
        int stageId,
        int quantity,
        string? observations,
        int actorUserId,
        string actorName,
        CancellationToken cancellationToken)
    {
        var stage = await _flow.GetStageByIdAsync(stageId, cancellationToken);
        if (stage is null) return ServiceResult.Fail("Etapa no encontrada.");
        if (quantity > stage.QuantityAvailable)
            return ServiceResult.Fail($"Solo hay {stage.QuantityAvailable} unidades disponibles.");

        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null) return ServiceResult.Fail("Orden no encontrada.");
        if (stage.ProductionOrderId != order.Id)
            return ServiceResult.Fail("La etapa no pertenece a la orden indicada.");

        stage.QuantityWithdrawn += quantity; // sale del WIP de la etapa hacia inventario terminado
        _flow.UpdateStage(stage);

        var fg = await _flow.GetFinishedGoodAsync(order.ProductName, cancellationToken);
        if (fg is null)
        {
            fg = new FinishedGoodStock { ProductName = order.ProductName, Stock = quantity };
            await _flow.AddFinishedGoodAsync(fg, cancellationToken);
        }
        else
        {
            fg.Stock += quantity;
            _flow.UpdateFinishedGood(fg);
        }

        await _flow.AddFinishedGoodMovementAsync(new FinishedGoodMovement
        {
            ProductName = order.ProductName,
            Quantity = quantity,
            ProductionOrderId = order.Id,
            StageId = stage.Id,
            ActorUserId = actorUserId,
            Observations = observations
        }, cancellationToken);

        await _flow.AddMovementAsync(new ProductionOrderStageMovement
        {
            ProductionOrderId = order.Id,
            FromStageId = stage.Id,
            MovementType = "InventoryIn",
            Quantity = quantity,
            ActorUserId = actorUserId,
            Observations = observations
        }, cancellationToken);

        // Avance de producción sin consumir BOM de nuevo (registro paralelo)
        var toAdd = Math.Min(quantity, Math.Max(0, order.TotalQuantity - order.ProducedQuantity));
        if (toAdd > 0)
        {
            order.ProducedQuantity += toAdd;
            if (order.ProducedQuantity >= order.TotalQuantity)
                order.Status = OrderStatus.Finalizada;
            _orders.Update(order);
        }

        await AddHistory(order.Id, ProductionHistoryEventType.PartialInventoryIn,
            $"Ingreso parcial a inventario: +{quantity} {order.ProductName} desde «{stage.Name}».",
            actorUserId, actorName, stage.Id, stage.Name, cancellationToken, quantity);

        return ServiceResult.Ok($"Ingresadas {quantity} unidades al inventario de producto terminado. Orden sigue abierta si falta meta.");
    }

    // Material que regresa a bodega desde una etapa MES + StockMovement Entrada (gap #14/#15)
    private async Task<ServiceResult> ApplyMaterialStageReentryAsync(
        int orderId,
        ProductionOrderStage stage,
        int materialId,
        int quantity,
        string? observations,
        int actorUserId,
        string actorName,
        CancellationToken cancellationToken)
    {
        if (quantity > stage.QuantityAvailable)
            return ServiceResult.Fail($"Solo hay {stage.QuantityAvailable} unidades disponibles.");

        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null) return ServiceResult.Fail("Orden no encontrada.");

        var material = await _materialRepository.GetByIdAsync(materialId, cancellationToken);
        if (material is null) return ServiceResult.Fail("Material no encontrado.");

        stage.QuantityWithdrawn += quantity;
        _flow.UpdateStage(stage);

        material.Stock += quantity;
        material.LastEntryDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _materialRepository.Update(material);

        var referencia = $"Orden:{order.Id}/Etapa:{stage.Name}";
        await _stockMovements.AddAsync(new StockMovement
        {
            MaterialId = material.Id,
            FechaUtc = DateTime.UtcNow,
            UsuarioId = actorUserId,
            TipoMovimiento = StockMovementType.Entrada,
            Cantidad = quantity,
            StockResultante = material.Stock,
            Referencia = referencia
        }, cancellationToken);

        await _flow.AddMovementAsync(new ProductionOrderStageMovement
        {
            ProductionOrderId = order.Id,
            FromStageId = stage.Id,
            MovementType = "InventoryIn",
            Quantity = quantity,
            ActorUserId = actorUserId,
            Observations = observations
        }, cancellationToken);

        await AddHistory(order.Id, ProductionHistoryEventType.PartialInventoryIn,
            $"Reingreso a bodega: +{quantity} «{material.Name}» desde «{stage.Name}».",
            actorUserId, actorName, stage.Id, stage.Name, cancellationToken, quantity);

        return ServiceResult.Ok($"Reingreso registrado: +{quantity} de «{material.Name}» desde «{stage.Name}».");
    }

    private async Task AddHistory(
        int orderId,
        ProductionHistoryEventType type,
        string message,
        int? actorUserId,
        string? actorName,
        int? stageId,
        string? stageName,
        CancellationToken cancellationToken,
        int? quantity = null)
    {
        await _flow.AddHistoryAsync(new ProductionOrderHistoryEntry
        {
            ProductionOrderId = orderId,
            EventType = type,
            Message = message,
            ActorUserId = actorUserId,
            ActorUserName = actorName,
            StageId = stageId,
            StageName = stageName,
            Quantity = quantity
        }, cancellationToken);
    }

    private static int ComputeFlowPercent(IReadOnlyList<ProductionOrderStage> stages)
    {
        if (stages.Count == 0) return 0;
        var done = stages.Count(s => s.Status == ProductionStageStatus.Finalizado);
        return Math.Min(100, (int)Math.Round(done * 100m / stages.Count));
    }

    private async Task<string> BuildMrpHintAsync(ProductionOrder order, CancellationToken cancellationToken)
    {
        var snapshots = await _snapshots.GetByOrderIdAsync(order.Id, cancellationToken);
        if (snapshots.Count > 0)
            return string.Join(", ", snapshots.Select(s =>
                $"{s.MaterialName}: {s.QuantityPerUnit} {UnitHelper.ToDisplay(s.Unit)}"));

        var bom = await _boms.GetByProductAsync(order.ProductName, cancellationToken);
        return bom.Count > 0
            ? string.Join(", ", bom.Select(b => $"{b.Material.Name}: {b.QuantityPerUnit} {UnitHelper.ToDisplay(b.Unit)}"))
            : "N/A";
    }

    private static OrderMaterialLineDto MapMaterialLine(ProductionOrderMaterialRequirement line)
    {
        var stock = line.Material?.Stock ?? 0;
        var pending = line.QuantityPending;
        var availability = line.IsFullyDelivered || pending <= 0
            ? MaterialStockAvailability.Suficiente
            : stock <= 0 ? MaterialStockAvailability.SinExistencias
            : stock < pending ? MaterialStockAvailability.Insuficiente
            : MaterialStockAvailability.Suficiente;

        return new OrderMaterialLineDto(
            line.Id, line.MaterialId, line.Material?.Code ?? "", line.Material?.Name ?? "",
            line.QuantityRequired, line.QuantityDelivered, pending, stock, stock - pending,
            UnitHelper.ToDisplay(line.Unit), line.Unit, line.Observations, availability, line.IsFullyDelivered);
    }
}
