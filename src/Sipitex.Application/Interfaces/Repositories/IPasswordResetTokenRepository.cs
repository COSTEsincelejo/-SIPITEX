using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

// Códigos de un solo uso (reset de contraseña y confirmación de correo)
public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    // Cuenta códigos de un propósito creados desde una fecha (rate limit)
    Task<int> CountCreatedSinceAsync(int userId, string purpose, DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PasswordResetToken>> GetUnusedByUserAsync(int userId, string purpose, CancellationToken cancellationToken = default);
    // El más reciente sin usar de ese propósito (cooldown y intentos fallidos)
    Task<PasswordResetToken?> GetLatestUnusedAsync(int userId, string purpose, CancellationToken cancellationToken = default);
    // Busca por hash sin filtrar vencimiento ni uso, para distinguir código malo, usado o vencido
    Task<PasswordResetToken?> FindByHashAsync(int userId, string purpose, string tokenHash, CancellationToken cancellationToken = default);
    void Update(PasswordResetToken token);
}
