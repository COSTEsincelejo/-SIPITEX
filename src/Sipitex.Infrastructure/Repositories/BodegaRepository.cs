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

    public Task<bool> ExistsByNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default,
        int? excludeId = null)
    {
        var normalized = nombre.Trim().ToLower();
        var query = _context.Bodegas.AsQueryable().Where(b => b.Nombre.ToLower() == normalized);
        if (excludeId is int id)
            query = query.Where(b => b.Id != id);
        return query.AnyAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _context.Bodegas.CountAsync(cancellationToken);

    public async Task<BodegaDependencias> CountDependenciasAsync(
        int bodegaId,
        CancellationToken cancellationToken = default)
    {
        var materiales = await _context.Materials.CountAsync(m => m.BodegaId == bodegaId, cancellationToken);
        var solicitudes = await _context.SolicitudesMaterial.CountAsync(s => s.BodegaId == bodegaId, cancellationToken);
        var bodegueros = await _context.Users.CountAsync(u => u.BodegaId == bodegaId, cancellationToken);
        return new BodegaDependencias(materiales, solicitudes, bodegueros);
    }

    public async Task AddAsync(Bodega bodega, CancellationToken cancellationToken = default) =>
        await _context.Bodegas.AddAsync(bodega, cancellationToken);

    public void Update(Bodega bodega) => _context.Bodegas.Update(bodega);

    public void Remove(Bodega bodega) => _context.Bodegas.Remove(bodega);
}
