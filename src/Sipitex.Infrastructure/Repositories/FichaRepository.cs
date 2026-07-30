using Microsoft.EntityFrameworkCore; // Include, AnyAsync, FirstOrDefaultAsync...
using Sipitex.Application.Interfaces.Repositories; // IFichaRepository
using Sipitex.Domain.Entities; // Ficha
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// CRUD de fichas de proceso (trazo, corte, confección...)
public class FichaRepository : IFichaRepository
{
    private readonly SipitexDbContext _context;

    public FichaRepository(SipitexDbContext context) => _context = context;

    // Todas las fichas con su orden de producción, ordenadas por código
    public async Task<IReadOnlyList<Ficha>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Fichas
            .Include(f => f.ProductionOrder) // Para mostrar OP-xxx en la lista
            .OrderBy(f => f.FichaCode)
            .ToListAsync(cancellationToken);

    // Busca una ficha por Id (para editar o ver detalle)
    public Task<Ficha?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Fichas
            .Include(f => f.ProductionOrder)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    // Para validar que no se repita el código al crear/editar
    public Task<bool> ExistsByCodeAsync(string fichaCode, CancellationToken cancellationToken = default) =>
        _context.Fichas.AnyAsync(
            f => f.FichaCode.ToLower() == fichaCode.ToLower(), // Comparo sin importar mayúsculas
            cancellationToken);

    // Inserta una ficha nueva (SaveChanges lo hace el UnitOfWork después)
    public async Task AddAsync(Ficha ficha, CancellationToken cancellationToken = default) =>
        await _context.Fichas.AddAsync(ficha, cancellationToken);

    // Marca la entidad como modificada para que EF la actualice al guardar
    public void Update(Ficha ficha) => _context.Fichas.Update(ficha);
}
