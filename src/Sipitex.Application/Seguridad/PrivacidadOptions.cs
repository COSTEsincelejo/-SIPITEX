namespace Sipitex.Application.Seguridad;

// Versión del texto publicado. Si la configuración no trae versión, se usa esta.
public static class PoliticaPrivacidad
{
    public const string Version = "1.1";

    public const string FechaActualizacion = "2026-11-06";

    public const string VersionAnterior = "1.0";

    public const string FechaVersionAnterior = "2026-10-15";
}

// Datos del responsable que la política muestra. Un campo vacío no se imprime.
public sealed class PrivacidadOptions
{
    public const string Seccion = "Privacidad";

    public string Version { get; set; } = PoliticaPrivacidad.Version;

    public string FechaActualizacion { get; set; } = PoliticaPrivacidad.FechaActualizacion;

    public string ResponsableNombre { get; set; } = "";

    public string CorreoContacto { get; set; } = "";

    public string Direccion { get; set; } = "";

    public string Telefono { get; set; } = "";

    public string Ciudad { get; set; } = "";

    public string NIT { get; set; } = "";

    public string HorarioAtencion { get; set; } = "";

    public string PlazoConservacion { get; set; } = "";

    public string ProveedorCorreo { get; set; } = "";

    public string PaisProveedorCorreo { get; set; } = "";

    public string ProveedorHosting { get; set; } = "";

    public string PaisProveedorHosting { get; set; } = "";

    public string AreaSolicitudes { get; set; } = "";

    public string UrlPoliticaInstitucional { get; set; } = "";

    public string VersionVigente() =>
        string.IsNullOrWhiteSpace(Version) ? PoliticaPrivacidad.Version : Version.Trim();

    public string FechaPublica() =>
        string.IsNullOrWhiteSpace(FechaActualizacion)
            ? PoliticaPrivacidad.FechaActualizacion
            : FechaActualizacion.Trim();
}
