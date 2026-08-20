using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

// Acceso a la tabla Bodegas. Alta y listado para el admin.
public class BodegaRepository : IBodegaRepository
{
    private readonly SipitexDbContext _context;

    public BodegaRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<Bodega>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Bodegas.AsNoTracking().OrderBy(b => b.Nombre).ToListAsync(cancellationToken);

    public Task<Bodega?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Bodegas.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsByNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var normalized = nombre.Trim().ToLower();
        return _context.Bodegas.AnyAsync(b => b.Nombre.ToLower() == normalized, cancellationToken);
    }

    public async Task AddAsync(Bodega bodega, CancellationToken cancellationToken = default) =>
        await _context.Bodegas.AddAsync(bodega, cancellationToken);
}
