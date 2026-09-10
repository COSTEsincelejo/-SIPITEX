using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class ProductLifecycleService : IProductLifecycleService
{
    private readonly IProductStateMachine _machine;
    private readonly IProductionOrderRepository _orders;
    private readonly IActivityLogService _activityLog;
    private readonly IUnitOfWork _unitOfWork;

    public ProductLifecycleService(
        IProductStateMachine machine,
        IProductionOrderRepository orders,
        IActivityLogService activityLog,
        IUnitOfWork unitOfWork)
    {
        _machine = machine;
        _orders = orders;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (actorUserId > 0)
        {
            await _activityLog.LogAsync(
                actorUserId,
                ActivityLogActions.ChangeProductState,
                ActivityLogEntities.ProductionOrder,
                entityId: order.Id.ToString(),
                details: $"{from}→{to}" + (hasJustificacion ? $"; {justificacion.Trim()}" : string.Empty),
                cancellationToken);
        }

        return ServiceResult.Ok($"Estado del producto en {order.OrderNumber}: {from} → {to}.");
    }
}
