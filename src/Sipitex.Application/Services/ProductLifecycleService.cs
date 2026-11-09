using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class ProductLifecycleService : IProductLifecycleService
{
    private readonly IProductStateMachine _machine;
    private readonly IProductionOrderRepository _orders;
    private readonly IPrendaTrazableRepository _prendas;
    private readonly IProductionFlowRepository _flow;
    private readonly IActivityLogService _activityLog;
    private readonly IUnitOfWork _unitOfWork;

    public ProductLifecycleService(
        IProductStateMachine machine,
        IProductionOrderRepository orders,
        IPrendaTrazableRepository prendas,
        IProductionFlowRepository flow,
        IActivityLogService activityLog,
        IUnitOfWork unitOfWork)
    {
        _machine = machine;
        _orders = orders;
        _prendas = prendas;
        _flow = flow;
        _activityLog = activityLog;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> TransitionAsync(
        int productionOrderId,
        EstadoProducto to,
        int actorUserId,
        string? justificacion = null,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(productionOrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("Orden de producción no encontrada.");

        var from = order.EstadoProducto;
        var hasJustificacion = !string.IsNullOrWhiteSpace(justificacion);
        if (!_machine.CanTransition(from, to, hasJustificacion, out var error))
            return ServiceResult.Fail(error ?? "Transición no permitida.");

        order.EstadoProducto = to;
        _orders.Update(order);
        await CopyToPrendasAsync(order.Id, to, cancellationToken);

        var fromIdx = Array.IndexOf(ProductStateMachine.Sequence, from);
        var toIdx = Array.IndexOf(ProductStateMachine.Sequence, to);
        if (toIdx >= 0 && fromIdx >= 0 && toIdx < fromIdx)
            await MoveCurrentStageToAsync(order, to, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (actorUserId > 0)
        {
            await _activityLog.LogAsync(
                actorUserId,
                ActivityLogActions.ChangeProductState,
                ActivityLogEntities.ProductionOrder,
                entityId: order.Id.ToString(),
                details: $"{from}→{to}" + (hasJustificacion ? $"; {justificacion!.Trim()}" : string.Empty),
                cancellationToken);
        }

        return ServiceResult.Ok($"Estado del producto en {order.OrderNumber}: {from} → {to}.");
    }

    public async Task AlignWithStageAsync(
        ProductionOrder order,
        string? stageName,
        CancellationToken cancellationToken = default)
    {
        // EstadoProducto no tiene Cancelado. La cancelación vive en OrderStatus.
        if (order.Status == OrderStatus.Cancelada)
            return;
        if (!EstadoProductoEtapa.TryForStage(stageName, out var target))
            return;

        if (order.EstadoProducto != target)
        {
            order.EstadoProducto = target;
            _orders.Update(order);
        }

        await CopyToPrendasAsync(order.Id, target, cancellationToken);
    }

    private async Task CopyToPrendasAsync(
        int productionOrderId,
        EstadoProducto target,
        CancellationToken cancellationToken)
    {
        var prendas = await _prendas.GetByOrderForUpdateAsync(productionOrderId, cancellationToken);
        foreach (var prenda in prendas)
        {
            if (prenda.Estado != target)
                prenda.Estado = target;
        }
    }

    // Un retroceso del ciclo (calidad Malo → Confección) deja la etapa actual en el mismo punto.
    private async Task MoveCurrentStageToAsync(
        ProductionOrder order,
        EstadoProducto target,
        CancellationToken cancellationToken)
    {
        var stages = await _flow.GetStagesByOrderAsync(order.Id, cancellationToken);
        var stage = stages
            .OrderBy(s => s.SortOrder)
            .FirstOrDefault(s => EstadoProductoEtapa.TryForStage(s.Name, out var mapped) && mapped == target);
        if (stage is null)
            return;

        if (stage.Status == ProductionStageStatus.Finalizado)
        {
            stage.Status = ProductionStageStatus.EnProceso;
            stage.CompletedAtUtc = null;
            _flow.UpdateStage(stage);
        }

        if (order.CurrentStageId == stage.Id)
            return;

        order.CurrentStageId = stage.Id;
        _orders.Update(order);
    }
}
