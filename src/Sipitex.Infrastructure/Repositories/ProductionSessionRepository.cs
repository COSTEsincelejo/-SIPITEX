using Microsoft.EntityFrameworkCore; // Include, OrderByDescending, Take...
using Sipitex.Application.Interfaces.Repositories; // IProductionSessionRepository
using Sipitex.Domain.Entities; // ProductionSession
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Sesiones diarias de producción que registra el instructor
public class ProductionSessionRepository : IProductionSessionRepository
{
    private readonly SipitexDbContext _context;

    public ProductionSessionRepository(SipitexDbContext context) => _context = context;

    // Las sesiones más recientes primero (para el dashboard o historial)
    public async Task<IReadOnlyList<ProductionSession>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default) =>
        await _context.ProductionSessions
            .Include(s => s.Ficha) // En qué ficha trabajó
            .Include(s => s.ProductionOrder) // Orden asociada
            .OrderByDescending(s => s.SessionDate) // Más nuevas arriba
            .Take(take) // Solo las últimas N
            .ToListAsync(cancellationToken);

    // Registra una sesión nueva del día
    public async Task AddAsync(ProductionSession session, CancellationToken cancellationToken = default) =>
        await _context.ProductionSessions.AddAsync(session, cancellationToken);
}
