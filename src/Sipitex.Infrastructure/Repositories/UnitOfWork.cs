using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

// Guarda todos los cambios del DbContext de una vez (patrón Unit of Work)
public class UnitOfWork : IUnitOfWork
{
    private readonly SipitexDbContext _context;

    public UnitOfWork(SipitexDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
