using System.Globalization;
using System.Text;

namespace Sipitex.Application.Helpers;

// Clave de comparación: sin tildes, en minúsculas y sin puntuación sobrante.
public static class TextoNormalizado
{
    public static string Clave(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lowered = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(lowered.Length);
        var pendingSpace = false;
        foreach (var c in lowered)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
            {
                if (pendingSpace && sb.Length > 0)
                    sb.Append(' ');
                pendingSpace = false;
                sb.Append(c);
                continue;
            }

            pendingSpace = true;
        }

        return sb.ToString();
    }

    // Encabezado de la plantilla: sin el texto entre paréntesis, sin tildes y con espacios colapsados.
    public static string Encabezado(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return Clave(QuitarParentesis(value));
    }

    private static string QuitarParentesis(string value)
    {
        var sb = new StringBuilder(value.Length);
        var depth = 0;
        foreach (var c in value)
        {
            if (c == '(')
            {
                depth++;
                continue;
            }

            if (c == ')' && depth > 0)
            {
                depth--;
                continue;
            }

            if (depth == 0)
                sb.Append(c);
        }

        return sb.ToString();
    }
}
