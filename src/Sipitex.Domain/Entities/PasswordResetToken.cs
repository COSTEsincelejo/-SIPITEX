namespace Sipitex.Domain.Entities;

// Token para recuperar la contraseña. Importante: en BD solo guardo el hash, nunca el token crudo.
public class PasswordResetToken
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // SHA-256 del token que se manda por correo. Si alguien ve la BD no puede usarlo.
    public string TokenHash { get; set; } = string.Empty;

    // Después de esta fecha ya no sirve
    public DateTime ExpiresAtUtc { get; set; }

    // Si ya se usó, queda marcada la fecha (null = todavía no se usó)
    public DateTime? UsedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
