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

    // Para validar que no se repita el código al crear/editar
    public Task<bool> ExistsByCodeAsync(string fichaCode, CancellationToken cancellationToken = default) =>
        _context.Fichas.AnyAsync(
            f => f.FichaCode.ToLower() == fichaCode.ToLower(),
            cancellationToken);

    public async Task AddAsync(Ficha ficha, CancellationToken cancellationToken = default) =>
        await _context.Fichas.AddAsync(ficha, cancellationToken);

    public void Update(Ficha ficha) => _context.Fichas.Update(ficha);
}
