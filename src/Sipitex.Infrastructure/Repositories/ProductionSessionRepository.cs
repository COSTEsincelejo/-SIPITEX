using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class ProductionSessionRepository : IProductionSessionRepository
{
    private readonly SipitexDbContext _context;

    public ProductionSessionRepository(SipitexDbContext context) => _context = context;

    // Las sesiones más recientes primero (para el dashboard o historial)
    public async Task<IReadOnlyList<ProductionSession>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default) =>
        await _context.ProductionSessions
            .Include(s => s.Ficha)
            .Include(s => s.ProductionOrder)
            .OrderByDescending(s => s.SessionDate)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ProductionSession session, CancellationToken cancellationToken = default) =>
        await _context.ProductionSessions.AddAsync(session, cancellationToken);
}
