namespace Sipitex.Application.Helpers;

/// <summary>Reglas de contraseña compartidas por alta de usuarios y reseteo.</summary>
public static class PasswordRules
{
    public const int MinLength = 6;

    /// <returns>Mensaje de error, o null si es válida.</returns>
    public static string? Validate(string? password, bool required)
    {
        if (required && string.IsNullOrWhiteSpace(password))
            return "La contraseña es obligatoria.";

        if (!string.IsNullOrWhiteSpace(password) && password.Length < MinLength)
            return $"La contraseña debe tener al menos {MinLength} caracteres.";

        return null;
    }
}
