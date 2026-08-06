using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Registro diario de unidades producidas por ficha
public interface IProductionSessionRepository
{
    // take = cuántas sesiones recientes traer
    Task<IReadOnlyList<ProductionSession>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default);

    // Sesiones con ficha, orden e instructores (para reporte de actividad)
    Task<IReadOnlyList<ProductionSession>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    Task AddAsync(ProductionSession session, CancellationToken cancellationToken = default);
}
