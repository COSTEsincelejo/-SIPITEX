using System.Security.Cryptography;
using System.Text;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Flujo de "olvidé mi contraseña" con token por correo
public class PasswordResetService : IPasswordResetService
{
    // El token dura 1 hora
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    // Ventana para limitar spam de solicitudes
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
    // Máximo 3 solicitudes por ventana
    private const int MaxRequestsPerWindow = 3;

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

    // Pide reset: genera token, invalida los viejos y manda el correo
    // No dice si el email existe (por seguridad)
    public async Task RequestResetAsync(string email, string publicBaseUrl, CancellationToken cancellationToken = default)
    {
        // Si falta email o URL base, salgo sin error (no revelar nada)
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(publicBaseUrl))
            return;

        // Normalizo correo para buscar en BD
        var normalizedEmail = email.Trim().ToLowerInvariant();
        // Query por email
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        // Usuario inexistente o inactivo: mismo comportamiento silencioso
        if (user is null || !user.IsActive)
            return;

        // Anti-spam: máximo 3 solicitudes en 15 min
        var now = DateTime.UtcNow;
        var recentCount = await _tokenRepository.CountCreatedSinceAsync(
            user.Id, now - RateLimitWindow, cancellationToken);
        if (recentCount >= MaxRequestsPerWindow)
            return;

        // Marco como usados los tokens que aún no se habían gastado
        var unused = await _tokenRepository.GetUnusedByUserAsync(user.Id, cancellationToken);
        foreach (var previous in unused)
        {
            previous.UsedAtUtc = now;
            _tokenRepository.Update(previous);
        }

        // Genero token nuevo en texto plano (solo va al correo)
        var plainToken = CreateSecureToken();
        // Entidad que guardo en BD (solo el hash)
        var entity = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(plainToken),
            ExpiresAtUtc = now.Add(TokenLifetime),
            UsedAtUtc = null,
            CreatedAtUtc = now
        };
        await _tokenRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Armo el link que va en el correo
        var baseUrl = publicBaseUrl.TrimEnd('/');
        var link =
            $"{baseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(plainToken)}&email={Uri.EscapeDataString(normalizedEmail)}";

        // Cuerpo del correo en texto plano
        var body =
            $"""
            Hola {user.Nombre},

            Recibimos una solicitud para restablecer su contraseña en SIPITEX.
            Use este enlace (válido por 1 hora, de un solo uso):

            {link}

            Si usted no solicitó este cambio, ignore este mensaje.
            """;

        // Envío por SMTP (si está configurado)
        await _emailSender.SendAsync(
            user.Email,
            user.Nombre,
            "SIPITEX — Restablecer contraseña",
            body,
            cancellationToken);
    }

    // Cambia la contraseña si el token sigue válido
    public async Task<ServiceResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        // Primero valido la nueva contraseña con las reglas comunes
        var passwordError = PasswordRules.Validate(newPassword, required: true);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        // Token y email no pueden venir vacíos
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return ServiceResult.Fail("Enlace inválido o expirado.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult.Fail("Enlace inválido o expirado.");

        var now = DateTime.UtcNow;
        // Busco el token hasheado en BD
        var hash = HashToken(token);
        var resetToken = await _tokenRepository.FindValidAsync(user.Id, hash, now, cancellationToken);
        if (resetToken is null)
            return ServiceResult.Fail("Enlace inválido o expirado.");

        // Actualizo contraseña y marco token como usado
        user.PasswordHash = PasswordHasher.Hash(newPassword);
        _userRepository.Update(user);

        resetToken.UsedAtUtc = now;
        _tokenRepository.Update(resetToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Contraseña actualizada. Ya puede iniciar sesión.");
    }

    // Guardamos el hash del token, nunca el token en texto plano en BD
    internal static string HashToken(string token)
    {
        // SHA256 del token en UTF-8
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    // Token aleatorio de 32 bytes
    private static string CreateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    // Base64 seguro para URLs (sin + ni /)
    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
