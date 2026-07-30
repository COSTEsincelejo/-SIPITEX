namespace Sipitex.Domain.Entities;

// Permisos extendidos que se pueden dar aparte del rol.
// Se guardan en User.PermisosExtendidos como texto separado por comas.
public static class ExtendedPermissions
{
    // Tipo de claim que meto en el cookie de autenticación
    public const string ClaimType = "permiso";

    public const string InventarioRegistrar = "Inventario.Registrar";
    public const string SolicitudesAprobar = "Solicitudes.Aprobar";
    public const string MrpSimular = "Mrp.Simular";
    public const string AlertasConfigurar = "Alertas.Configurar";

    // Lista blanca: solo estos valores son válidos
    public static readonly string[] All =
    [
        InventarioRegistrar,
        SolicitudesAprobar,
        MrpSimular,
        AlertasConfigurar
    ];

    // Para mostrar en la UI con un label legible
    public static readonly (string Key, string Label)[] Catalog =
    [
        (InventarioRegistrar, "Registrar materiales en inventario"),
        (SolicitudesAprobar, "Aprobar / rechazar solicitudes"),
        (MrpSimular, "Simular MRP"),
        (AlertasConfigurar, "Configurar / evaluar alertas")
    ];

    // Saca solo los permisos válidos del string crudo (ignora basura o typos)
    public static IReadOnlyList<string> Parse(string? raw) =>
        (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => All.Contains(p, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    // Vuelve a armar el string limpio para guardar en BD
    public static string Serialize(IEnumerable<string>? permissions) =>
        string.Join(", ", Parse(string.Join(",", permissions ?? [])));
}
