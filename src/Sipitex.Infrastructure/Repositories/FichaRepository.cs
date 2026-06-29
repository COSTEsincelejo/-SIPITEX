using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class FichaRepository : IFichaRepository
{
    private readonly SipitexDbContext _context;

    public FichaRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<Ficha>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Fichas
            .Include(f => f.ProductionOrder)
            .OrderBy(f => f.FichaCode)
            .ToListAsync(cancellationToken);

    public Task<Ficha?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Fichas
            .Include(f => f.ProductionOrder)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public void Update(Ficha ficha) => _context.Fichas.Update(ficha);
}
