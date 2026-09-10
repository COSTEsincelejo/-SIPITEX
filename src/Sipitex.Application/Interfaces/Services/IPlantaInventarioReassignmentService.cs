using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Reasignación de stock/solicitudes antes de dar de baja una planta de inventario.
public interface IPlantaInventarioReassignmentService
{
    Task<ServiceResult> ReassignAndDeleteAsync(
        int origenId,
        int destinoId,
        CancellationToken cancellationToken = default);
}
