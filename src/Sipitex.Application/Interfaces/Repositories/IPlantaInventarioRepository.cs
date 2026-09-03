using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Catálogo de plantas de inventario (Planta 1 / Planta 2 seed + altas del admin)
public interface IPlantaInventarioRepository
{
    Task<IReadOnlyList<PlantaInventario>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlantaInventario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNombreAsync(string nombre, CancellationToken cancellationToken = default, int? excludeId = null);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<PlantaInventarioDependencias> CountDependenciasAsync(int plantaInventarioId, CancellationToken cancellationToken = default);
    Task AddAsync(PlantaInventario planta, CancellationToken cancellationToken = default);
    void Update(PlantaInventario planta);
    void Remove(PlantaInventario planta);
}

public readonly record struct PlantaInventarioDependencias(int Materiales, int Solicitudes, int Encargados)
{
    public bool Any => Materiales > 0 || Solicitudes > 0 || Encargados > 0;
}
