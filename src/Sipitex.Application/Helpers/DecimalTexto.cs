using System.Globalization;

namespace Sipitex.Application.Helpers;

// Acepta 65.4 y 65,4. Vacío es null: no convierte la ausencia en cero.
public static class DecimalTexto
{
    public static bool TryParseOpcional(string? raw, string etiqueta, out decimal? valor, out string? error)
    {
        valor = null;
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        var crudo = raw.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (crudo.Contains(',') && crudo.Contains('.'))
            crudo = crudo.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
        else if (crudo.Contains(','))
            crudo = crudo.Replace(',', '.');

        if (!decimal.TryParse(crudo, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            error = $"{etiqueta} no es un número válido.";
            return false;
        }

        if (parsed < 0)
        {
            error = $"{etiqueta} no puede ser negativo.";
            return false;
        }

        try
        {
            valor = decimal.Round(parsed, 2, MidpointRounding.AwayFromZero);
            return true;
        }
        catch (OverflowException)
        {
            error = $"{etiqueta} no es un número válido.";
            return false;
        }
    }

    public static string? Formato(decimal? valor) =>
        valor?.ToString("0.##", CultureInfo.InvariantCulture);
}
