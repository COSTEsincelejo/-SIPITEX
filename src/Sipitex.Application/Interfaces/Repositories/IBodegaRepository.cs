using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Catálogo de bodegas (Bodega 1 / Bodega 2 seed + altas del admin)
public interface IBodegaRepository
{
    Task<IReadOnlyList<Bodega>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Bodega?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNombreAsync(string nombre, CancellationToken cancellationToken = default, int? excludeId = null);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<BodegaDependencias> CountDependenciasAsync(int bodegaId, CancellationToken cancellationToken = default);
    Task AddAsync(Bodega bodega, CancellationToken cancellationToken = default);
    void Update(Bodega bodega);
    void Remove(Bodega bodega);
}

public readonly record struct BodegaDependencias(int Materiales, int Solicitudes, int Bodegueros)
{
    public bool Any => Materiales > 0 || Solicitudes > 0 || Bodegueros > 0;
}
