using System.Text.RegularExpressions;

namespace Sipitex.Application.Seguridad;

// Quita de textos de auditoría valores que parezcan clave, token o código.
public static partial class RedactorDatosSensibles
{
    private static readonly HashSet<string> Propiedades = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "PasswordHash", "NewPassword", "ConfirmPassword",
        "Token", "TokenHash", "Code", "Codigo", "VerificationCode", "Secret"
    };

    public static bool EsPropiedadSensible(string nombre) =>
        Propiedades.Contains(nombre);

    public static string? Limpiar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return texto;

        return Secreto().Replace(texto, "$1=[omitido]");
    }

    [GeneratedRegex(@"(?i)(password|contraseña|token|c[oó]digo|secret)\s*[=:]\s*\S+")]
    private static partial Regex Secreto();
}
