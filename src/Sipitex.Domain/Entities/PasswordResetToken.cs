namespace Sipitex.Domain.Entities;

// Código de un solo uso (recuperar contraseña o confirmar correo).
// En BD solo guardo el hash, nunca el código en claro.
public class PasswordResetToken
{
    // PK
    public int Id { get; set; }

    // Usuario dueño del código
    public int UserId { get; set; }

    // Navegación al usuario
    public User User { get; set; } = null!;

    // SHA-256 del código de 6 dígitos que va en el correo
    public string TokenHash { get; set; } = string.Empty;

    // PasswordReset o EmailConfirmation (un código de un flujo no sirve en el otro)
    public string Purpose { get; set; } = VerificationCodePurpose.PasswordReset;

    // Intentos fallidos de este código. Al llegar al tope se invalida.
    public int FailedAttempts { get; set; }

    // Después de esta fecha el código ya no sirve
    public DateTime ExpiresAtUtc { get; set; }

    // null = todavía no se usó; si tiene fecha, ya se gastó
    public DateTime? UsedAtUtc { get; set; }

    // Cuándo se creó (sirve también para el rate limit y el cooldown de reenvío)
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

// Distingue los dos flujos que comparten la tabla PasswordResetTokens
public static class VerificationCodePurpose
{
    public const string PasswordReset = "PasswordReset";
    public const string EmailConfirmation = "EmailConfirmation";
}
