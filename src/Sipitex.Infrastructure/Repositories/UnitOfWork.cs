using Microsoft.EntityFrameworkCore; // SaveChangesAsync
using Sipitex.Application.Interfaces; // IUnitOfWork
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Guarda todos los cambios del DbContext de una vez (patrón Unit of Work)
public class UnitOfWork : IUnitOfWork
{
    private readonly SipitexDbContext _context; // Un solo contexto por request

    public UnitOfWork(SipitexDbContext context) => _context = context;

    // Persiste todo lo que los repos fueron agregando/modificando
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
