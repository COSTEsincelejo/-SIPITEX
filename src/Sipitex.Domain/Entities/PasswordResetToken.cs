namespace Sipitex.Domain.Entities;

// Token de "olvidé mi contraseña". En BD solo guardo el hash, nunca el token crudo.
public class PasswordResetToken
{
    // PK
    public int Id { get; set; }

    // Usuario que pidió el reset
    public int UserId { get; set; }

    // Navegación al usuario
    public User User { get; set; } = null!;

    // SHA-256 del token que va en el link del correo
    public string TokenHash { get; set; } = string.Empty;

    // Después de esta fecha el link ya no sirve
    public DateTime ExpiresAtUtc { get; set; }

    // null = todavía no se usó; si tiene fecha, ya se gastó
    public DateTime? UsedAtUtc { get; set; }

    // Cuándo se creó (sirve también para el rate limit)
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
