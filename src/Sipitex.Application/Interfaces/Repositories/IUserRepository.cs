using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Usuarios del sistema (login, roles, permisos)
public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    // excludeUserId sirve al editar para no chocar con el propio correo
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);
    void Add(User user);
    void Update(User user);
    void Remove(User user);

    Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken = default);

    // Motivos que impiden hard-delete (historial / FKs de auditoría)
    Task<IReadOnlyList<string>> GetDeletionBlockersAsync(int userId, CancellationToken cancellationToken = default);
}
