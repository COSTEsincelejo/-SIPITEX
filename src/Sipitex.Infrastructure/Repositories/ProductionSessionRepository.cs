using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class ProductionSessionRepository : IProductionSessionRepository
{
    private readonly SipitexDbContext _context;

    public ProductionSessionRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductionSession>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default) =>
        await _context.ProductionSessions
            .Include(s => s.Ficha)
            .Include(s => s.ProductionOrder)
            .OrderByDescending(s => s.SessionDate)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductionSession>> GetInDateRangeAsync(DateTime fromInclusive, DateTime toExclusive, CancellationToken cancellationToken = default) =>
        await _context.ProductionSessions
            .Include(s => s.Ficha)
            .Include(s => s.ProductionOrder)
            .Where(s => s.SessionDate >= fromInclusive && s.SessionDate < toExclusive)
            .OrderByDescending(s => s.SessionDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ProductionSession session, CancellationToken cancellationToken = default) =>
        await _context.ProductionSessions.AddAsync(session, cancellationToken);
}
