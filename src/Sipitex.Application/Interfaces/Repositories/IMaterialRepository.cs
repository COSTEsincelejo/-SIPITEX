using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Catálogo de materiales e inventario
public interface IMaterialRepository
{
    Task<IReadOnlyList<Material>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Material> Items, int TotalCount, int TotalSinFiltro, int Page)> PageAsync(
        int? plantaInventarioId,
        IReadOnlyCollection<int>? allowedPlantaIds,
        string? nombre,
        string? nivel,
        int? page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(int PlantaInventarioId, int Materiales, int Bajo, int Critico)>> SummarizeByPlantaAsync(
        IReadOnlyCollection<int>? allowedPlantaIds,
        CancellationToken cancellationToken = default);
    Task<Material?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Material material, CancellationToken cancellationToken = default);
    void Update(Material material);
    void Remove(Material material);
}
