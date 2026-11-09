using System.Text.RegularExpressions;

namespace Sipitex.Web.Logging;

// Quita contraseñas y cadenas de conexión antes de escribir un fallo de arranque.
public static partial class RegistroSeguro
{
    public static void Fatal(ILogger logger, Exception ex, string paso)
    {
        var texto = ex.ToString();
        if (TieneSecreto(texto))
        {
            logger.LogCritical(
                "Arranque abortado en el paso {Paso}. No se reintenta. {Detalle}",
                paso,
                Redactar(texto));
            return;
        }

        logger.LogCritical(ex, "Arranque abortado en el paso {Paso}. No se reintenta.", paso);
    }

    public static bool TieneSecreto(string texto) =>
        PasswordPar().IsMatch(texto) || UserInfo().IsMatch(texto) || CadenaNpgsql().IsMatch(texto);

    public static string Redactar(string texto)
    {
        var limpio = PasswordPar().Replace(texto, "$1=***");
        limpio = UserInfo().Replace(limpio, "$1***:***@");
        return CadenaNpgsql().Replace(limpio, "Host=***");
    }

    [GeneratedRegex(@"(?i)(password|pwd)\s*=\s*[^;\s""']+", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordPar();

    [GeneratedRegex(@"(?i)(://)[^/\s:@]+:[^/\s@]+@", RegexOptions.CultureInvariant)]
    private static partial Regex UserInfo();

    [GeneratedRegex(@"(?i)\b(?:Host|Server)\s*=\s*[^;\s]+(?:\s*;\s*[^;\r\n]+){0,12}", RegexOptions.CultureInvariant)]
    private static partial Regex CadenaNpgsql();
}
