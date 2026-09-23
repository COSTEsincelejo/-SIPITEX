using System.Security.Cryptography;
using System.Text;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Códigos numéricos de un solo uso para recuperar contraseña y confirmar correo.
// Reutiliza PasswordResetToken: en BD queda el hash, nunca el código.
// El código dura 15 minutos (antes el enlace duraba 1 hora). Un código de 6 dígitos
// es mucho más corto que un token de 32 bytes, así que la ventana corta y el tope
// de intentos compensan ese riesgo.
public class PasswordResetService : IPasswordResetService
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    private const int MaxRequestsPerWindow = 3;
    public const int MaxFailedAttempts = 5;

    public const string InvalidCodeMessage = "El código no es válido.";
    public const string ExpiredCodeMessage = "El código expiró. Solicite uno nuevo.";
    public const string UsedCodeMessage = "El código ya fue utilizado. Solicite uno nuevo.";
    public const string TooManyAttemptsMessage = "Demasiados intentos. Solicite un código nuevo.";
    public const string ResendCooldownMessage = "Espere un minuto antes de reenviar el código.";
    public const string ConfirmationPendingMessage =
        "Si hay una cuenta pendiente de confirmación, enviamos un código nuevo.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    public PasswordResetService(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IUnitOfWork unitOfWork,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }

    // Pide reset: genera un código de 6 dígitos, invalida los anteriores y lo manda por correo.
    // No dice si el email existe (por seguridad) y no escribe el código en ningún log.
    public async Task RequestResetAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
            return;

        await IssueCodeAsync(
            user,
            VerificationCodePurpose.PasswordReset,
            "SIPITEX — Código para restablecer contraseña",
            plainCode =>
                $"""
                Hola {user.Nombre},

                Recibimos una solicitud para restablecer su contraseña en SIPITEX.
                Ingrese este código de un solo uso en la pantalla de restablecimiento:

                {plainCode}

                El código vence en 15 minutos.
                Si usted no solicitó este cambio, ignore este mensaje.
                """,
            enforceCooldown: false,
            cancellationToken);
    }

    // Cambia la contraseña si el código sigue válido. No revela si el correo existe.
    public async Task<ServiceResult> ResetPasswordAsync(
        string email,
        string code,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var passwordError = PasswordRules.Validate(newPassword, required: true);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        var outcome = await EvaluateCodeAsync(
            email,
            code,
            VerificationCodePurpose.PasswordReset,
            user =>
            {
                user.PasswordHash = PasswordHasher.Hash(newPassword);
                _userRepository.Update(user);
            },
            cancellationToken);

        if (!outcome.Success)
            return outcome;

        return ServiceResult.Ok("Contraseña actualizada. Ya puede iniciar sesión.");
    }

    public async Task<ServiceResult> SendEmailConfirmationAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user.Id <= 0)
            return ServiceResult.Fail("El usuario debe estar guardado antes de enviar la confirmación.");

        if (!user.IsActive || user.EmailConfirmed)
            return ServiceResult.Ok();

        var status = await IssueCodeAsync(
            user,
            VerificationCodePurpose.EmailConfirmation,
            "SIPITEX — Confirme su correo",
            ConfirmationBody(user.Nombre),
            enforceCooldown: false,
            cancellationToken);

        return status == CodeIssueStatus.RateLimited
            ? ServiceResult.Fail("No se pudo enviar el código de confirmación en este momento.")
            : ServiceResult.Ok();
    }

    public async Task<ServiceResult> ConfirmEmailAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ServiceResult.Fail(InvalidCodeMessage);

        var user = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult.Fail(InvalidCodeMessage);

        if (user.EmailConfirmed)
            return ServiceResult.Ok("El correo ya está confirmado. Ya puede iniciar sesión.");

        var outcome = await EvaluateCodeAsync(
            email,
            code,
            VerificationCodePurpose.EmailConfirmation,
            confirmed =>
            {
                confirmed.EmailConfirmed = true;
                _userRepository.Update(confirmed);
            },
            cancellationToken);

        if (!outcome.Success)
            return outcome;

        return ServiceResult.Ok("Correo confirmado. Ya puede iniciar sesión.");
    }

    public async Task<ServiceResult> ResendEmailConfirmationAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ServiceResult.Fail("El correo es obligatorio.");

        var user = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive || user.EmailConfirmed)
            return ServiceResult.Ok(ConfirmationPendingMessage);

        var status = await IssueCodeAsync(
            user,
            VerificationCodePurpose.EmailConfirmation,
            "SIPITEX — Confirme su correo",
            ConfirmationBody(user.Nombre),
            enforceCooldown: true,
            cancellationToken);

        return status switch
        {
            CodeIssueStatus.Cooldown => ServiceResult.Fail(ResendCooldownMessage),
            CodeIssueStatus.RateLimited => ServiceResult.Fail("Demasiadas solicitudes. Espere unos minutos e intente de nuevo."),
            _ => ServiceResult.Ok(ConfirmationPendingMessage)
        };
    }

    // Guardamos el hash del código, nunca el código en texto plano en BD
    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static Func<string, string> ConfirmationBody(string nombre) =>
        plainCode =>
            $"""
            Hola {nombre},

            Se creó su cuenta en SIPITEX. Para poder iniciar sesión, confirme su correo con este código de un solo uso:

            {plainCode}

            El código vence en 15 minutos.
            Si usted no esperaba este mensaje, ignorelo.
            """;

    private async Task<CodeIssueStatus> IssueCodeAsync(
        User user,
        string purpose,
        string subject,
        Func<string, string> bodyForCode,
        bool enforceCooldown,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var recentCount = await _tokenRepository.CountCreatedSinceAsync(
            user.Id, purpose, now - RateLimitWindow, cancellationToken);
        if (recentCount >= MaxRequestsPerWindow)
            return CodeIssueStatus.RateLimited;

        if (enforceCooldown)
        {
            var latest = await _tokenRepository.GetLatestUnusedAsync(user.Id, purpose, cancellationToken);
            if (latest is not null && now - latest.CreatedAtUtc < ResendCooldown)
                return CodeIssueStatus.Cooldown;
        }

        var unused = await _tokenRepository.GetUnusedByUserAsync(user.Id, purpose, cancellationToken);
        foreach (var previous in unused)
        {
            previous.UsedAtUtc = now;
            _tokenRepository.Update(previous);
        }

        var plainCode = CreateSixDigitCode();
        var entity = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(plainCode),
            Purpose = purpose,
            FailedAttempts = 0,
            ExpiresAtUtc = now.Add(CodeLifetime),
            UsedAtUtc = null,
            CreatedAtUtc = now
        };
        await _tokenRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // El código solo viaja en el cuerpo del correo. No se registra en logs ni en ActivityLog.
        await _emailSender.SendAsync(
            user.Email,
            user.Nombre,
            subject,
            bodyForCode(plainCode),
            cancellationToken);

        return CodeIssueStatus.Sent;
    }

    private async Task<ServiceResult> EvaluateCodeAsync(
        string email,
        string code,
        string purpose,
        Action<User> onSuccess,
        CancellationToken cancellationToken)
    {
        var normalizedCode = NormalizeCode(code);
        if (string.IsNullOrWhiteSpace(email) || !IsSixDigitCode(normalizedCode))
            return ServiceResult.Fail(InvalidCodeMessage);

        var user = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult.Fail(InvalidCodeMessage);

        var now = DateTime.UtcNow;
        var match = await _tokenRepository.FindByHashAsync(
            user.Id, purpose, HashToken(normalizedCode), cancellationToken);

        if (match is not null)
        {
            if (match.UsedAtUtc is not null)
                return ServiceResult.Fail(UsedCodeMessage);

            if (match.ExpiresAtUtc <= now)
                return ServiceResult.Fail(ExpiredCodeMessage);

            if (match.FailedAttempts >= MaxFailedAttempts)
            {
                match.UsedAtUtc = now;
                _tokenRepository.Update(match);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return ServiceResult.Fail(TooManyAttemptsMessage);
            }

            onSuccess(user);
            match.UsedAtUtc = now;
            _tokenRepository.Update(match);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok();
        }

        var current = await _tokenRepository.GetLatestUnusedAsync(user.Id, purpose, cancellationToken);
        if (current is null)
            return ServiceResult.Fail(InvalidCodeMessage);

        if (current.ExpiresAtUtc <= now)
            return ServiceResult.Fail(ExpiredCodeMessage);

        current.FailedAttempts++;
        if (current.FailedAttempts >= MaxFailedAttempts)
        {
            current.UsedAtUtc = now;
            _tokenRepository.Update(current);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Fail(TooManyAttemptsMessage);
        }

        _tokenRepository.Update(current);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Fail(InvalidCodeMessage);
    }

    // 000000–999999, con ceros a la izquierda. RandomNumberGenerator evita el Random predecible.
    private static string CreateSixDigitCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        return new string(code.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    private static bool IsSixDigitCode(string code) =>
        code.Length == 6 && code.All(char.IsDigit);

    private enum CodeIssueStatus
    {
        Sent,
        RateLimited,
        Cooldown
    }
}
