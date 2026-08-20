using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

// Acceso a SolicitudMaterial / Detalle / Entrega (flujo Ficha, multi-ítem)
public class SolicitudMaterialRepository : ISolicitudMaterialRepository
{
    private readonly SipitexDbContext _context;

    public SolicitudMaterialRepository(SipitexDbContext context) => _context = context;

    public async Task AddAsync(SolicitudMaterial solicitud, CancellationToken cancellationToken = default) =>
        await _context.SolicitudesMaterial.AddAsync(solicitud, cancellationToken);

    public Task<SolicitudMaterial?> GetByIdWithDetallesAsync(int id, CancellationToken cancellationToken = default) =>
        _context.SolicitudesMaterial
            .Include(s => s.Ficha)
            .Include(s => s.Solicitante)
            .Include(s => s.Bodega)
            .Include(s => s.Detalles)
            .ThenInclude(d => d.Material)
            .Include(s => s.Entrega)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SolicitudMaterial>> GetAllWithFichaAsync(CancellationToken cancellationToken = default) =>
        await _context.SolicitudesMaterial
            .Include(s => s.Ficha)
            .Include(s => s.Solicitante)
            .Include(s => s.Bodega)
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync(cancellationToken);

    public Task<DetalleSolicitudMaterial?> GetDetalleByIdAsync(int detalleId, CancellationToken cancellationToken = default) =>
        _context.DetallesSolicitudMaterial
            .Include(d => d.Material)
            .Include(d => d.SolicitudMaterial)
            .ThenInclude(s => s.Detalles)
            .FirstOrDefaultAsync(d => d.Id == detalleId, cancellationToken);

    public void Update(SolicitudMaterial solicitud) =>
        _context.SolicitudesMaterial.Update(solicitud);

    public async Task AddEntregaAsync(EntregaMaterial entrega, CancellationToken cancellationToken = default) =>
        await _context.EntregasMaterial.AddAsync(entrega, cancellationToken);

    public Task<string?> GetLastCodigoSolicitudAsync(CancellationToken cancellationToken = default) =>
        _context.SolicitudesMaterial
            .OrderByDescending(s => s.Codigo)
            .Select(s => s.Codigo)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<string?> GetLastCodigoEntregaAsync(CancellationToken cancellationToken = default) =>
        _context.EntregasMaterial
            .OrderByDescending(e => e.Codigo)
            .Select(e => e.Codigo)
            .FirstOrDefaultAsync(cancellationToken);
}
