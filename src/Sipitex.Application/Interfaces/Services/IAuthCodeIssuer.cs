using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Services;

public enum AuthCodeIssueStatus
{
    Issued,
    RateLimited,
    Cooldown
}

public interface IAuthCodeIssuer
{
    Task<AuthCodeIssueStatus> TryIssueAsync(
        User user,
        string purpose,
        TimeSpan lifetime,
        string subject,
        string instructions,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> ConsumeAsync(
        int userId,
        string purpose,
        string plainCode,
        CancellationToken cancellationToken = default);
}

public static class AuthCodeMessages
{
    public const string InvalidOrExpired = "Código inválido o expirado.";
    public const string TooManyAttempts = "Demasiados intentos fallidos. Solicite un código nuevo.";
    public const string Cooldown = "Espere un minuto antes de solicitar otro código.";
    public const string GenericResetSent = "Si el correo existe, se envió un código.";
    public const string EmailNotVerified = "Debe verificar su correo para iniciar sesión.";
    public const string PasswordUpdated = "Contraseña actualizada. Ya puede iniciar sesión.";
    public const string EmailVerified = "Correo verificado. Ya puede iniciar sesión.";
}
