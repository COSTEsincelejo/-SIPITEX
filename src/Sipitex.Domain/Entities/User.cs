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

/// <summary>
/// Permisos que el administrador puede otorgar (sobre todo a instructores).
/// Se guardan en User.PermisosExtendidos separados por coma.
/// </summary>
public static class UserPermissions
{
    public const string ClaimType = "permission";

    public const string FuncionesAdministrador = "FuncionesAdministrador";
    public const string RegistrarMateriales = "RegistrarMateriales";
    public const string GestionInventario = "GestionInventario";
    public const string VerReportes = "VerReportes";
    public const string RegistrarProduccion = "RegistrarProduccion";
    public const string AprobarSolicitudes = "AprobarSolicitudes";

    public static readonly (string Code, string Label, string Hint)[] Catalog =
    [
        (FuncionesAdministrador, "Funciones de administrador", "Permite al instructor registrar materiales y gestionar inventario como un admin operativo."),
        (RegistrarMateriales, "Registrar materiales", "Solo alta de productos/materias primas en inventario."),
        (GestionInventario, "Gestionar inventario", "Ajustar stock, estado y aprobar/rechazar solicitudes."),
        (VerReportes, "Ver reportes", "Descargar reportes PDF/Excel."),
        (RegistrarProduccion, "Registrar producción", "Registrar sesiones en fichas."),
        (AprobarSolicitudes, "Aprobar solicitudes", "Aprobar o rechazar salidas de bodega.")
    ];

    public static IReadOnlyList<string> Parse(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    public static string Join(IEnumerable<string>? permissions) =>
        string.Join(", ", (permissions ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
}
