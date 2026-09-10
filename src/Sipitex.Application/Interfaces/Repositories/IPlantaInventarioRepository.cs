using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Catálogo de plantasInventario (PlantaInventario 1 / PlantaInventario 2 seed + altas del admin)
public interface IPlantaInventarioRepository
{
    Task<IReadOnlyList<PlantaInventario>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlantaInventario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNombreAsync(string nombre, CancellationToken cancellationToken = default, int? excludeId = null);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountActivasAsync(CancellationToken cancellationToken = default);
    Task<PlantaInventarioDependencias> CountDependenciasAsync(int plantaInventarioId, CancellationToken cancellationToken = default);
    Task<PlantaInventarioReassignmentResult> ReassignDependenciasAsync(
        int origenId,
        int destinoId,
        CancellationToken cancellationToken = default);
    Task AddAsync(PlantaInventario plantaInventario, CancellationToken cancellationToken = default);
    void Update(PlantaInventario plantaInventario);
    void Remove(PlantaInventario plantaInventario);
}

public readonly record struct PlantaInventarioDependencias(
    int Materiales,
    int Solicitudes,
    int Encargados,
    decimal StockTotal = 0)
{
    public bool Any => Materiales > 0 || Solicitudes > 0 || Encargados > 0;
    public bool HasStock => StockTotal > 0;
}

public readonly record struct PlantaInventarioReassignmentResult(
    int Materiales,
    int Movimientos,
    int Solicitudes,
    int Encargados,
    decimal StockTotal);
