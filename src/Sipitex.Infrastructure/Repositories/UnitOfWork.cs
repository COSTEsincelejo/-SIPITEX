using Microsoft.EntityFrameworkCore; // BeginTransactionAsync, SaveChangesAsync
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

    // Transacción explícita para operaciones atómicas (p. ej. aprobar ítem + descontar stock)
    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
