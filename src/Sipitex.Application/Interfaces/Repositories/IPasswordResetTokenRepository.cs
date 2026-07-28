using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task<int> CountCreatedSinceAsync(int userId, DateTime sinceUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PasswordResetToken>> GetUnusedByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> FindValidAsync(int userId, string tokenHash, DateTime utcNow, CancellationToken cancellationToken = default);
    void Update(PasswordResetToken token);
}
