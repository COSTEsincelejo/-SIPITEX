namespace Sipitex.Domain.Entities;

// Constancia de que la persona aceptó una versión de la política de tratamiento.
// Los usuarios que ya existían no tienen fila: se les pide aceptación al volver a entrar.
public class AceptacionTratamiento
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // Coincide con Privacidad:Version. Si cambia, hay que aceptar de nuevo.
    public string VersionPolitica { get; set; } = string.Empty;

    public DateTime AceptadaEnUtc { get; set; }

    // IP vista por la app (puede ser la del proxy). Nula si el socket no la trae.
    public string? DireccionIp { get; set; }
}
