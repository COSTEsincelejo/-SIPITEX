namespace Sipitex.Domain.Entities;

// Entidad de usuario del sistema. Acá guardo lo básico para login y roles.
public class User
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Nunca guardo la contraseña en texto plano, solo el hash
    public string PasswordHash { get; set; } = string.Empty;

    // Por defecto lo dejo como Instructor (es el rol más común en el taller)
    public string Rol { get; set; } = UserRoles.Instructor;

    // Si es instructor, puede tener una ficha asignada (nullable porque el admin/bodeguero no tienen)
    public int? FichaAsignadaId { get; set; }
    public Ficha? FichaAsignada { get; set; }

    // Permisos extra separados por comas, ej: "Inventario.Registrar, Mrp.Simular"
    // Los parseo con ExtendedPermissions.Parse
    public string PermisosExtendidos { get; set; } = string.Empty;

    // Ruta relativa de la foto, algo como /uploads/profiles/1.jpg
    public string? PhotoPath { get; set; }

    // El mismo usuario escribe qué hace en su rol (lo pedían para el perfil)
    public string? FuncionDescripcion { get; set; }

    // Si está en false no puede entrar al sistema
    public bool IsActive { get; set; } = true;
}

// Constantes de roles para no andar escribiendo strings sueltos por todo el código
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

    // El admin no se puede crear desde la UI, solo Instructor y Bodeguero
    // (el primer admin sale del seed)
    public static readonly string[] CreatableByAdmin =
    [
        Instructor,
        Bodeguero
    ];
}
