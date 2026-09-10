namespace Sipitex.Domain.Entities;

// Entidad de usuario: login, rol, foto y permisos extra
public class User
{
    // PK de la tabla Users
    public int Id { get; set; }

    // Nombre completo para mostrar en la UI y en claims
    public string Nombre { get; set; } = string.Empty;

    // Correo único (con eso hacen login)
    public string Email { get; set; } = string.Empty;

    // Hash de la contraseña (nunca el texto plano)
    public string PasswordHash { get; set; } = string.Empty;

    // Rol del sistema; por defecto Instructor porque es el más común en el taller
    public string Rol { get; set; } = UserRoles.Instructor;

    // FK opcional a la ficha "principal" del instructor (null en admin/encargadoDeBodega)
    public int? FichaAsignadaId { get; set; }

    // Navegación a esa ficha (EF Core la llena si hago Include)
    public Ficha? FichaAsignada { get; set; }

    // Asignaciones M2M a plantasInventario (solo aplica a Rol == EncargadoDeBodega; vacío en el resto)
    public ICollection<UserPlantaInventario> UserPlantasInventario { get; set; } = [];

    public IReadOnlyList<int> GetAssignedPlantaInventarioIds() =>
        UserPlantasInventario
            .Select(ub => ub.PlantaInventarioId)
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

    public string FormatAssignedPlantasInventario()
    {
        if (UserPlantasInventario.Count == 0)
            return "—";

        return string.Join(", ",
            UserPlantasInventario
                .Select(ub => ub.PlantaInventario?.Nombre ?? $"#{ub.PlantaInventarioId}")
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
    }

    // Permisos extra en texto, separados por comas (los parseo con ExtendedPermissions)
    public string PermisosExtendidos { get; set; } = string.Empty;

    // Ruta web de la foto, ej: /uploads/profiles/1_abc.jpg (null = sin foto)
    public string? PhotoPath { get; set; }

    // Texto libre de qué hace en su rol (lo escribe él en el perfil)
    public string? FuncionDescripcion { get; set; }

    // false = no puede entrar aunque tenga contraseña correcta
    public bool IsActive { get; set; } = true;
}

// Constantes de roles para no escribir strings sueltos por todo el proyecto
public static class UserRoles
{
    // Valor exacto que se guarda en User.Rol y en ClaimTypes.Role
    public const string Administrador = "Administrador";
    public const string Instructor = "Instructor";
    public const string EncargadoDeBodega = "Encargado de bodega";

    // Todos los roles válidos (para validar en ediciones)
    public static readonly string[] All =
    [
        Administrador,
        Instructor,
        EncargadoDeBodega
    ];

    // Roles que el admin puede crear/asignar desde la UI (incluye Administrador — gap #1)
    public static readonly string[] CreatableByAdmin =
    [
        Administrador,
        Instructor,
        EncargadoDeBodega
    ];
}
