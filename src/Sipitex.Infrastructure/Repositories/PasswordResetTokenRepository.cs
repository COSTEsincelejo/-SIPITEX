using Microsoft.EntityFrameworkCore; // CountAsync, Where, FirstOrDefaultAsync...
using Sipitex.Application.Interfaces.Repositories; // IPasswordResetTokenRepository
using Sipitex.Domain.Entities; // PasswordResetToken
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Repositories;

// Tokens de recuperación de contraseña (solo guardamos el hash)
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly SipitexDbContext _context;

    public PasswordResetTokenRepository(SipitexDbContext context) => _context = context;

    // Guarda un token nuevo cuando el usuario pide reset
    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();

    // Rate limiting: cuántos tokens pidió el usuario desde cierta fecha
    public Task<int> CountCreatedSinceAsync(int userId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens.CountAsync(
            t => t.UserId == userId && t.CreatedAtUtc >= sinceUtc,
            cancellationToken);

    // Tokens que aún no se usaron (para invalidarlos al generar uno nuevo)
    public async Task<IReadOnlyList<PasswordResetToken>> GetUnusedByUserAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAtUtc == null) // UsedAtUtc null = vigente
            .ToListAsync(cancellationToken);

    // Busca un token válido (no usado y no expirado)
    public Task<PasswordResetToken?> FindValidAsync(
        int userId,
        string tokenHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens.FirstOrDefaultAsync(
            t => t.UserId == userId
                 && t.TokenHash == tokenHash // Comparo con el hash, no el token en claro
                 && t.UsedAtUtc == null
                 && t.ExpiresAtUtc > utcNow, // Todavía no venció
            cancellationToken);

    // Marca el token como usado después del reset exitoso
    public void Update(PasswordResetToken token) => _context.PasswordResetTokens.Update(token);
}
