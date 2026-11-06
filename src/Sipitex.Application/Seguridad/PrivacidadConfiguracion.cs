namespace Sipitex.Application.Seguridad;

// Avisos de arranque. La página oculta el dato; el log le dice al operador qué falta.
public static class PrivacidadConfiguracion
{
    public static IReadOnlyList<string> Advertencias(PrivacidadOptions opciones)
    {
        var avisos = new List<string>();
        Falta(avisos, "CorreoContacto", "el correo de contacto del responsable", opciones.CorreoContacto);
        Falta(avisos, "Direccion", "la dirección del responsable", opciones.Direccion);
        Falta(avisos, "Telefono", "el teléfono del responsable", opciones.Telefono);
        Falta(avisos, "NIT", "el NIT del responsable", opciones.NIT);
        Falta(avisos, "HorarioAtencion", "el horario de atención", opciones.HorarioAtencion);
        Falta(avisos, "PlazoConservacion", "el plazo de conservación", opciones.PlazoConservacion);
        Falta(avisos, "ProveedorCorreo", "el nombre del proveedor de correo", opciones.ProveedorCorreo);
        Falta(avisos, "PaisProveedorCorreo", "el país del proveedor de correo", opciones.PaisProveedorCorreo);
        Falta(avisos, "ProveedorHosting", "el nombre del proveedor de hosting y base de datos", opciones.ProveedorHosting);
        Falta(avisos, "PaisProveedorHosting", "el país del proveedor de hosting", opciones.PaisProveedorHosting);
        Falta(avisos, "AreaSolicitudes", "el área o cargo que atiende las solicitudes de datos", opciones.AreaSolicitudes);
        Falta(avisos, "ResponsableNombre", "el nombre del responsable", opciones.ResponsableNombre);
        Falta(avisos, "Ciudad", "la ciudad del responsable", opciones.Ciudad);

        if (string.IsNullOrWhiteSpace(opciones.UrlPoliticaInstitucional))
        {
            Falta(avisos, "UrlPoliticaInstitucional", "el enlace a la política institucional del SENA", opciones.UrlPoliticaInstitucional);
        }
        else if (UrlPublica(opciones.UrlPoliticaInstitucional) is null)
        {
            avisos.Add(
                "El enlace a la política institucional no es una dirección http o https. " +
                "La página no lo muestra (Privacidad:UrlPoliticaInstitucional).");
        }

        return avisos;
    }

    public static string? UrlPublica(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;
        if (!Uri.TryCreate(valor.Trim(), UriKind.Absolute, out var uri))
            return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return null;
        return uri.ToString();
    }

    private static void Falta(List<string> avisos, string clave, string descripcion, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
            return;

        avisos.Add(
            $"Falta {descripcion} (Privacidad:{clave}). La página de la política no muestra esa línea.");
    }
}
