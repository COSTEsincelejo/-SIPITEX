using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Catálogo de materiales e inventario
public interface IMaterialRepository
{
    Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken cancellationToken = default);

    // Detalle de planta: ignora el filtro global y acota por PlantaInventarioId.
    Task<IReadOnlyList<MaterialPlantaStockDto>> ListStockByPlantaDetalleAsync(
        int plantaInventarioId,
        CancellationToken cancellationToken = default);
    Task<Material?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Material material, CancellationToken cancellationToken = default);
    void Update(Material material);
    void Remove(Material material);
}
