using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Códigos de un solo uso (verificación de correo y reset de contraseña)
public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task<int> CountCreatedSinceAsync(int userId, string purpose, DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PasswordResetToken>> GetUnusedByUserAsync(int userId, string purpose, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> FindLatestUnusedAsync(int userId, string purpose, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> FindValidAsync(int userId, string purpose, string tokenHash, DateTime utcNow, CancellationToken cancellationToken = default);
    void Update(PasswordResetToken token);
}
