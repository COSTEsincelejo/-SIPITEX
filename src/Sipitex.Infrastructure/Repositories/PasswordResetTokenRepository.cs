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

    // Rate limiting: cuántos tokens pidió el usuario desde cierta fecha
    public Task<int> CountCreatedSinceAsync(int userId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens.CountAsync(
            t => t.UserId == userId && t.CreatedAtUtc >= sinceUtc,
            cancellationToken);

    public async Task<IReadOnlyList<PasswordResetToken>> GetUnusedByUserAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

    // Busca un token válido (no usado y no expirado)
    public Task<PasswordResetToken?> FindValidAsync(
        int userId,
        string tokenHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens.FirstOrDefaultAsync(
            t => t.UserId == userId
                 && t.TokenHash == tokenHash
                 && t.UsedAtUtc == null
                 && t.ExpiresAtUtc > utcNow,
            cancellationToken);

    public void Update(PasswordResetToken token) => _context.PasswordResetTokens.Update(token);
}
