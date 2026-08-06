namespace Sipitex.Application.Interfaces;

// Guarda cambios en BD de una sola vez (patrón Unit of Work)
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Ejecuta la acción dentro de una transacción DB y hace commit al final.
    // Si la acción lanza, hace rollback sin dejar cambios parciales.
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
