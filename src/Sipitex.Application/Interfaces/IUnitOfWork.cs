namespace Sipitex.Application.Interfaces;

// Guarda cambios en BD de una sola vez (patrón Unit of Work)
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
