namespace Sipitex.Application.Seguridad;

// Minutos de inactividad antes de cerrar la cookie. Sección "Seguridad" de appsettings.
public sealed class SeguridadOptions
{
    public const string Seccion = "Seguridad";

    public int MinutosInactividad { get; set; } = 20;

    public int MinutosEfectivos()
    {
        // Menos de 2 minutos haría que el aviso de 60 segundos no quepa.
        return MinutosInactividad < 2 ? 2 : MinutosInactividad;
    }
}
