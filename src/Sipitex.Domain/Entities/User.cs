namespace Sipitex.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = UserRoles.Instructor;
    public int? FichaAsignadaId { get; set; }
    public Ficha? FichaAsignada { get; set; }
    public string PermisosExtendidos { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public static class UserRoles
{
    public const string Administrador = "Administrador";
    public const string Instructor = "Instructor";
    public const string Bodeguero = "Bodeguero";

    public static readonly string[] All =
    [
        Administrador,
        Instructor,
        Bodeguero
    ];
}
