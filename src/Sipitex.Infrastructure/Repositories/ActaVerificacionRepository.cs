using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

// Persistencia de actas de verificación
public class ActaVerificacionRepository : IActaVerificacionRepository
{
    private readonly SipitexDbContext _context;

    public ActaVerificacionRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<ActaVerificacion>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.ActasVerificacion
            .Include(a => a.ProductionOrder)
            .Include(a => a.Ficha)
                .ThenInclude(f => f.Instructors)
            .Include(a => a.Instructor)
            .OrderByDescending(a => a.FechaObservacion)
            .ToListAsync(cancellationToken);

    public async Task<ActaVerificacion?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.ActasVerificacion
            .Include(a => a.ProductionOrder)
            .Include(a => a.Ficha)
                .ThenInclude(f => f.Instructors)
            .Include(a => a.Instructor)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(ActaVerificacion acta, CancellationToken cancellationToken = default) =>
        await _context.ActasVerificacion.AddAsync(acta, cancellationToken);

    public void Update(ActaVerificacion acta) => _context.ActasVerificacion.Update(acta);
}
