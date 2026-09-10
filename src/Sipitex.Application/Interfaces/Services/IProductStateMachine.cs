using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Interfaces.Services;

public interface IProductStateMachine
{
    bool CanTransition(EstadoProducto from, EstadoProducto to, bool justificacionRetroceso, out string? error);
    IReadOnlyList<EstadoProducto> AllowedTransitions(EstadoProducto from, bool allowBackward);
}

public interface IProductLifecycleService
{
    Task<ServiceResult> TransitionAsync(
        int productionOrderId,
        EstadoProducto to,
        int actorUserId,
        string? justificacion = null,
        CancellationToken cancellationToken = default);
}
