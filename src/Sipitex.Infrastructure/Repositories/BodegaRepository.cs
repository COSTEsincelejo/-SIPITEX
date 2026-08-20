using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

// Lectura del catálogo de bodegas (seed Fase 1)
public class BodegaRepository : IBodegaRepository
{
    private readonly SipitexDbContext _context;

    public BodegaRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<Bodega>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Bodegas
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken);

    public Task<Bodega?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Bodegas.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
}
