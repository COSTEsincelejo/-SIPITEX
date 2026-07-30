namespace Sipitex.Domain.Entities;

// Permisos extra aparte del rol. Se guardan en User.PermisosExtendidos separados por comas.
public static class ExtendedPermissions
{
    // Nombre del claim que meto en la cookie (ClaimTypes custom)
    public const string ClaimType = "permiso";

    // Claves fijas de cada permiso (tienen que coincidir con las policies)
    public const string InventarioRegistrar = "Inventario.Registrar";
    public const string SolicitudesAprobar = "Solicitudes.Aprobar";
    public const string MrpSimular = "Mrp.Simular";
    public const string AlertasConfigurar = "Alertas.Configurar";

    // Lista blanca: si no está acá, Parse lo tira
    public static readonly string[] All =
    [
        InventarioRegistrar,   // puede dar de alta materiales
        SolicitudesAprobar,    // puede aprobar/rechazar pedidos a bodega
        MrpSimular,            // puede correr la simulación MRP
        AlertasConfigurar      // puede disparar evaluación de alertas
    ];

    // Para armar checkboxes en la UI con un label legible
    public static readonly (string Key, string Label)[] Catalog =
    [
        (InventarioRegistrar, "Registrar materiales en inventario"),
        (SolicitudesAprobar, "Aprobar / rechazar solicitudes"),
        (MrpSimular, "Simular MRP"),
        (AlertasConfigurar, "Configurar / evaluar alertas")
    ];

    // Limpia el string crudo de BD y deja solo permisos válidos
    public static IReadOnlyList<string> Parse(string? raw) =>
        // Si viene null, trato como vacío para no petar
        (raw ?? string.Empty)
            // Parto por comas y quito espacios / vacíos
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            // Solo dejo lo que esté en la lista blanca
            .Where(p => All.Contains(p, StringComparer.Ordinal))
            // Sin duplicados
            .Distinct(StringComparer.Ordinal)
            // Materializo la lista
            .ToList();

    // Arma de nuevo el string limpio para guardar en User.PermisosExtendidos
    public static string Serialize(IEnumerable<string>? permissions) =>
        // Uno los permisos con ", ", pasando antes por Parse para filtrar basura
        // Si permissions es null, uso array vacío
        string.Join(", ", Parse(string.Join(",", permissions ?? [])));
}
