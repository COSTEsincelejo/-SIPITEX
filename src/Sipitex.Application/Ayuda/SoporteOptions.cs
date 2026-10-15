namespace Sipitex.Application.Ayuda;

// Correo y responsable del centro de ayuda. Se leen de la sección "Soporte" de appsettings.
public sealed class SoporteOptions
{
    public const string Seccion = "Soporte";

    public string Responsable { get; set; } = "";

    public string Correo { get; set; } = "";
}
