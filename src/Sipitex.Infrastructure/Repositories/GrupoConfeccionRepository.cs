using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class GrupoConfeccionRepository : IGrupoConfeccionRepository
{
    private readonly SipitexDbContext _context;

    public GrupoConfeccionRepository(SipitexDbContext context) => _context = context;

    public async Task AddAsync(GrupoConfeccion grupo, CancellationToken cancellationToken = default) =>
        await _context.GruposConfeccion.AddAsync(grupo, cancellationToken);

    public void Update(GrupoConfeccion grupo) => _context.GruposConfeccion.Update(grupo);

    public Task<GrupoConfeccion?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.GruposConfeccion
            .Include(g => g.ProductionOrder)
            .Include(g => g.Instructor)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<GrupoConfeccion>> GetByOrderIdAsync(
        int productionOrderId,
        CancellationToken cancellationToken = default) =>
        await _context.GruposConfeccion
            .AsNoTracking()
            .Include(g => g.ProductionOrder)
            .Include(g => g.Instructor)
            .Where(g => g.ProductionOrderId == productionOrderId)
            .OrderByDescending(g => g.FechaRealizacion)
            .ThenByDescending(g => g.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GrupoConfeccion>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.GruposConfeccion
            .AsNoTracking()
            .Include(g => g.ProductionOrder)
            .Include(g => g.Instructor)
            .OrderByDescending(g => g.FechaRealizacion)
            .ThenByDescending(g => g.Id)
            .ToListAsync(cancellationToken);
}
