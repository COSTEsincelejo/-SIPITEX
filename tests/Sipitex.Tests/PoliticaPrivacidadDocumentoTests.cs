using Sipitex.Application.Seguridad;

namespace Sipitex.Tests;

public class PoliticaPrivacidadDocumentoTests
{
    [Fact]
    public void SinConfiguracion_NoMuestraAppsettingsNiMarcadores()
    {
        var texto = PoliticaPrivacidadDocumento.Crear(new PrivacidadOptions()).ATexto();

        Assert.DoesNotContain("appsettings", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Pendiente de completar", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[COMPLETAR", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Correo de contacto:", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Dirección de notificación:", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Teléfono de contacto:", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("NIT:", texto, StringComparison.Ordinal);
        Assert.Contains("Ley 1581 de 2012", texto, StringComparison.Ordinal);
        Assert.Contains("Decreto 1074 de 2015", texto, StringComparison.Ordinal);
        Assert.Contains("1.0 (2026-10-15)", texto, StringComparison.Ordinal);
        Assert.Contains("1.1 (2026-11-06)", texto, StringComparison.Ordinal);
        Assert.Contains("no afirma que la base esté inscrita", texto, StringComparison.Ordinal);

        foreach (var titulo in TitulosVigentes)
            Assert.Contains(titulo, texto, StringComparison.Ordinal);
    }

    [Fact]
    public void ConConfiguracion_MuestraLosValoresYLaTransmisionInternacional()
    {
        var opciones = new PrivacidadOptions
        {
            CorreoContacto = "datos@ejemplo.test",
            Direccion = "Calle 1",
            Telefono = "6050000000",
            NIT = "900000000-1",
            HorarioAtencion = "lunes a viernes, 8 a 12",
            PlazoConservacion = "5 años de ejemplo",
            ProveedorCorreo = "Proveedor Correo",
            PaisProveedorCorreo = "Colombia",
            ProveedorHosting = "Proveedor Host",
            PaisProveedorHosting = "Chile",
            AreaSolicitudes = "Coordinación del centro",
            UrlPoliticaInstitucional = "https://ejemplo.test/politica"
        };

        var texto = PoliticaPrivacidadDocumento.Crear(opciones).ATexto();

        Assert.Contains("Correo de contacto: datos@ejemplo.test", texto, StringComparison.Ordinal);
        Assert.Contains("Dirección de notificación: Calle 1", texto, StringComparison.Ordinal);
        Assert.Contains("Teléfono de contacto: 6050000000", texto, StringComparison.Ordinal);
        Assert.Contains("NIT: 900000000-1", texto, StringComparison.Ordinal);
        Assert.Contains("Proveedor Host, Chile", texto, StringComparison.Ordinal);
        Assert.Contains("transmisión o transferencia internacional", texto, StringComparison.Ordinal);
        Assert.Contains("https://ejemplo.test/politica", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Proveedor Correo, Colombia", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Advertencias_SiFaltanContacto_NoMencionanAppsettings()
    {
        var avisos = PrivacidadConfiguracion.Advertencias(new PrivacidadOptions());

        Assert.Contains(avisos, a => a.Contains("Privacidad:CorreoContacto", StringComparison.Ordinal));
        Assert.Contains(avisos, a => a.Contains("Privacidad:Direccion", StringComparison.Ordinal));
        Assert.Contains(avisos, a => a.Contains("Privacidad:Telefono", StringComparison.Ordinal));
        Assert.DoesNotContain(avisos, a => a.Contains("appsettings", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Advertencias_ConContacto_NoRepiteEsasClaves()
    {
        var avisos = PrivacidadConfiguracion.Advertencias(new PrivacidadOptions
        {
            CorreoContacto = "datos@ejemplo.test",
            Direccion = "Calle 1",
            Telefono = "6050000000"
        });

        Assert.DoesNotContain(avisos, a => a.Contains("CorreoContacto", StringComparison.Ordinal));
        Assert.DoesNotContain(avisos, a => a.Contains("Privacidad:Direccion", StringComparison.Ordinal));
        Assert.DoesNotContain(avisos, a => a.Contains("Privacidad:Telefono", StringComparison.Ordinal));
    }

    private static readonly string[] TitulosVigentes =
    [
        "Introducción y alcance",
        "Definiciones",
        "Responsable del tratamiento",
        "Datos que se recopilan",
        "Menores de edad",
        "Finalidades",
        "Base legal y autorización",
        "Con quién se comparten los datos",
        "Conservación y supresión",
        "Derechos del titular",
        "Cómo ejercer los derechos y plazos",
        "Efectos de revocar la autorización",
        "Seguridad",
        "Cookies y analítica",
        "Cambios a la política y control de versiones",
        "Registro Nacional de Bases de Datos"
    ];
}
