namespace Sipitex.Domain.Entities;

/// <summary>
/// Claves de permisos extendidos (serializados en <see cref="User.PermisosExtendidos"/> separados por comas).
/// </summary>
public static class ExtendedPermissions
{
    public const string ClaimType = "permiso";

    public const string InventarioRegistrar = "Inventario.Registrar";
    public const string SolicitudesAprobar = "Solicitudes.Aprobar";
    public const string MrpSimular = "Mrp.Simular";
    public const string AlertasConfigurar = "Alertas.Configurar";

    public static readonly string[] All =
    [
        InventarioRegistrar,
        SolicitudesAprobar,
        MrpSimular,
        AlertasConfigurar
    ];

    public static readonly (string Key, string Label)[] Catalog =
    [
        (InventarioRegistrar, "Registrar materiales en inventario"),
        (SolicitudesAprobar, "Aprobar / rechazar solicitudes"),
        (MrpSimular, "Simular MRP"),
        (AlertasConfigurar, "Configurar / evaluar alertas")
    ];

    public static IReadOnlyList<string> Parse(string? raw) =>
        (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => All.Contains(p, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public static string Serialize(IEnumerable<string>? permissions) =>
        string.Join(", ", Parse(string.Join(",", permissions ?? [])));
}
