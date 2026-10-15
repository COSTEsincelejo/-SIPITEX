namespace Sipitex.Application.Seguridad;

// Datos del responsable del tratamiento. Si un campo queda vacío, la página lo marca como pendiente.
public sealed class PrivacidadOptions
{
    public const string Seccion = "Privacidad";

    public string Version { get; set; } = "1.0";

    public string FechaActualizacion { get; set; } = "";

    public string ResponsableNombre { get; set; } = "";

    public string CorreoContacto { get; set; } = "";

    public string Direccion { get; set; } = "";

    public string Telefono { get; set; } = "";

    public string Ciudad { get; set; } = "";

    public string VersionVigente() =>
        string.IsNullOrWhiteSpace(Version) ? "1.0" : Version.Trim();
}
