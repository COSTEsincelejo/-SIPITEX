using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

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

    public Task<PasswordResetToken?> FindLatestUnusedAsync(
        int userId,
        string purpose,
        CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAtUtc == null)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PasswordResetToken?> FindValidAsync(
        int userId,
        string purpose,
        string tokenHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens.FirstOrDefaultAsync(
            t => t.UserId == userId
                 && t.Purpose == purpose
                 && t.TokenHash == tokenHash
                 && t.UsedAtUtc == null
                 && t.ExpiresAtUtc > utcNow,
            cancellationToken);

    public void Update(PasswordResetToken token) => _context.PasswordResetTokens.Update(token);
}
