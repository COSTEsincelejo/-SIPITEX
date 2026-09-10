namespace Sipitex.Application.Interfaces.Services;

// Contador de intentos de login fallidos (email + IP) con bloqueo temporal.
public interface ILoginAttemptGuard
{
    bool IsLockedOut(string email, string? ipAddress);
    void RecordFailure(string email, string? ipAddress);
    void Reset(string email, string? ipAddress);
}

public static class LoginAttemptMessages
{
    public const string InvalidCredentials = "Credenciales inválidas.";
    public const string LockedOut = "Demasiados intentos fallidos. Intente de nuevo más tarde.";
}
