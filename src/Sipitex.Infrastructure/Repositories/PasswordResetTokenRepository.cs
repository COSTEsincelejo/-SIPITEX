using Microsoft.EntityFrameworkCore; // CountAsync, Where, FirstOrDefaultAsync...
using Sipitex.Application.Interfaces.Repositories; // IPasswordResetTokenRepository
using Sipitex.Domain.Entities; // PasswordResetToken
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Códigos de recuperación y de confirmación de correo (solo guardamos el hash)
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly SipitexDbContext _context;

    public PasswordResetTokenRepository(SipitexDbContext context) => _context = context;

    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();

    public Task<int> CountCreatedSinceAsync(
        int userId,
        string purpose,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens.CountAsync(
            t => t.UserId == userId && t.Purpose == purpose && t.CreatedAtUtc >= sinceUtc,
            cancellationToken);

    public async Task<IReadOnlyList<PasswordResetToken>> GetUnusedByUserAsync(
        int userId,
        string purpose,
        CancellationToken cancellationToken = default) =>
        await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

    public Task<PasswordResetToken?> GetLatestUnusedAsync(
        int userId,
        string purpose,
        CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAtUtc == null)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PasswordResetToken?> FindByHashAsync(
        int userId,
        string purpose,
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.TokenHash == tokenHash)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public void Update(PasswordResetToken token) => _context.PasswordResetTokens.Update(token);
}
