namespace Sipitex.Domain.Entities;

// Foto de perfil en la base (bytea). El disco de Render se borra en cada deploy.
public class UserProfilePhoto
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public byte[] Content { get; set; } = [];

    public string ContentType { get; set; } = "application/octet-stream";

    public string FileName { get; set; } = "foto";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
