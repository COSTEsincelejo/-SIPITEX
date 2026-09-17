namespace Sipitex.Domain.Entities;

// Código de un solo uso enviado por correo (verificación o reset). En BD solo el hash.
public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // SHA-256 del código de 6 dígitos (nunca el valor en claro)
    public string TokenHash { get; set; } = string.Empty;

    public string Purpose { get; set; } = AuthCodePurposes.PasswordReset;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int FailedAttempts { get; set; }
}
