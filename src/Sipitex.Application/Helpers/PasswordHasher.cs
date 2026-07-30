using System.Security.Cryptography;

namespace Sipitex.Application.Helpers;

// Hash de contraseñas con PBKDF2 (no guardamos la clave en texto plano)
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    // Genera salt aleatorio y devuelve todo en un string con formato propio
    public static string Hash(string password)
    {
        // Salt único por contraseña
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        // Derivo la clave con PBKDF2 y SHA256
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        // Formato: pbkdf2$iteraciones$salt$key en base64
        return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    // Vuelve a hashear con el mismo salt y compara en tiempo constante
    public static bool Verify(string password, string storedHash)
    {
        // Parto el string guardado por $
        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
        if (!int.TryParse(parts[1], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        // Hasheo la contraseña ingresada con el mismo salt
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        // Comparación segura contra timing attacks
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
