using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Tokens de un solo uso para reset de contraseña
public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    // Cuenta tokens creados desde una fecha (rate limit)
    Task<int> CountCreatedSinceAsync(int userId, DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PasswordResetToken>> GetUnusedByUserAsync(int userId, CancellationToken cancellationToken = default);
    // Busca token válido por hash (no expirado ni usado)
    Task<PasswordResetToken?> FindValidAsync(int userId, string tokenHash, DateTime utcNow, CancellationToken cancellationToken = default);
    void Update(PasswordResetToken token);
}
