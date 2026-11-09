using Microsoft.EntityFrameworkCore; // BeginTransactionAsync, SaveChangesAsync
using Sipitex.Application.Interfaces; // IUnitOfWork
using Sipitex.Domain; // ConflictoConcurrenciaStockException
using Sipitex.Domain.Entities; // Material
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Guarda todos los cambios del DbContext de una vez (patrón Unit of Work)
public class UnitOfWork : IUnitOfWork
{
    private readonly SipitexDbContext _context; // Un solo contexto por request

    public UnitOfWork(SipitexDbContext context) => _context = context;

    // Persiste todo lo que los repos fueron agregando/modificando
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex) when (EsMaterial(ex))
        {
            throw new ConflictoConcurrenciaStockException();
        }
    }

    // Transacción explícita para operaciones atómicas (p. ej. aprobar ítem + descontar stock).
    // Debe ir dentro de CreateExecutionStrategy porque Npgsql RetryOnFailure no admite
    // transacciones iniciadas por el usuario fuera de la estrategia.
    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action(cancellationToken);
                await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private static bool EsMaterial(DbUpdateConcurrencyException ex) =>
        ex.Entries.Any(e => e.Entity is Material);
}
