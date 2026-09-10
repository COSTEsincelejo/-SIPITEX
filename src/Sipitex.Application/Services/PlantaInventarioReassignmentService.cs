using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

// Mueve materiales (y con ellos el historial de StockMovement), solicitudes y
// asignaciones de encargados a una planta destino, luego da de baja lógica el origen.
public class PlantaInventarioReassignmentService : IPlantaInventarioReassignmentService
{
    private const int DefaultPlantaInventarioId = 1;

    private readonly IPlantaInventarioRepository _plantas;
    private readonly IUnitOfWork _unitOfWork;

    public PlantaInventarioReassignmentService(
        IPlantaInventarioRepository plantas,
        IUnitOfWork unitOfWork)
    {
        _plantas = plantas;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> ReassignAndDeleteAsync(
        int origenId,
        int destinoId,
        CancellationToken cancellationToken = default)
    {
        if (origenId == destinoId)
            return ServiceResult.Fail("La planta de destino debe ser distinta a la de origen.");

        var origen = await _plantas.GetByIdAsync(origenId, cancellationToken);
        if (origen is null || !origen.Activo)
            return ServiceResult.Fail("Planta de inventario de origen no encontrada.");

        var destino = await _plantas.GetByIdAsync(destinoId, cancellationToken);
        if (destino is null || !destino.Activo)
            return ServiceResult.Fail("Planta de inventario de destino no encontrada o está inactiva.");

        if (origenId == DefaultPlantaInventarioId)
            return ServiceResult.Fail(
                "No se puede eliminar Planta de Inventario 1: el sistema la usa como planta de inventario por defecto al crear solicitudes de insumos libres.");

        if (await _plantas.CountActivasAsync(cancellationToken) <= 1)
            return ServiceResult.Fail("No se puede eliminar la última planta de inventario activa del sistema.");

        PlantaInventarioReassignmentResult moved = default;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            moved = await _plantas.ReassignDependenciasAsync(origenId, destinoId, ct);
            origen.Activo = false;
            _plantas.Update(origen);
        }, cancellationToken);

        return ServiceResult.Ok(
            $"Stock y dependencias de «{origen.Nombre}» se reasignaron a «{destino.Nombre}» " +
            $"({moved.Materiales} material(es), stock {moved.StockTotal}, {moved.Movimientos} movimiento(s) de historial conservados). " +
            "La planta de origen quedó inactiva.");
    }
}
