using System.Text.RegularExpressions;

namespace Sipitex.Application.Helpers;

// Primer filtro de formato. La prueba real de que el correo existe es el código enviado.
public static partial class EmailFormat
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SimpleEmail();

    public static bool IsValid(string? email) =>
        !string.IsNullOrWhiteSpace(email) && SimpleEmail().IsMatch(email.Trim());
}
