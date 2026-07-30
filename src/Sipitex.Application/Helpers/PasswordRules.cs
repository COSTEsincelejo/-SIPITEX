namespace Sipitex.Application.Helpers;

// Reglas de contraseña compartidas por alta de usuarios y reseteo
public static class PasswordRules
{
    // Longitud mínima que pedimos en el sistema
    public const int MinLength = 6;

    // Devuelve mensaje de error o null si está bien
    public static string? Validate(string? password, bool required)
    {
        // Si es obligatoria y viene vacía, error
        if (required && string.IsNullOrWhiteSpace(password))
            return "La contraseña es obligatoria.";

        // Si mandaron algo pero es muy corta, también error
        if (!string.IsNullOrWhiteSpace(password) && password.Length < MinLength)
            return $"La contraseña debe tener al menos {MinLength} caracteres.";

        return null;
    }
}
