using System.Security.Cryptography;
using System.Text;

namespace Sipitex.Application.Helpers;

// Códigos numéricos de 6 dígitos: se hashean igual que los tokens de reset.
public static class AuthCodeHelper
{
    public const int Length = 6;
    public const int MaxFailedAttempts = 5;
    public const int MaxRequestsPerWindow = 3;

    public static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    public static string Generate() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        var digits = new char[Length];
        var n = 0;
        foreach (var ch in code)
        {
            if (char.IsDigit(ch))
            {
                if (n >= Length)
                    return string.Empty;
                digits[n++] = ch;
            }
        }

        return n == Length ? new string(digits) : string.Empty;
    }

    public static string Hash(string code)
    {
        var normalized = Normalize(code);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }
}
