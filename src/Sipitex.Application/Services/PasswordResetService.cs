using System.Security.Cryptography;
using System.Text;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);
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

    public async Task RequestResetAsync(string email, string publicBaseUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(publicBaseUrl))
            return;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
            return;

        var now = DateTime.UtcNow;
        var recentCount = await _tokenRepository.CountCreatedSinceAsync(
            user.Id, now - RateLimitWindow, cancellationToken);
        if (recentCount >= MaxRequestsPerWindow)
            return;

        var unused = await _tokenRepository.GetUnusedByUserAsync(user.Id, cancellationToken);
        foreach (var previous in unused)
        {
            previous.UsedAtUtc = now;
            _tokenRepository.Update(previous);
        }

        var plainToken = CreateSecureToken();
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

        var baseUrl = publicBaseUrl.TrimEnd('/');
        var link =
            $"{baseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(plainToken)}&email={Uri.EscapeDataString(normalizedEmail)}";

        var body =
            $"""
            Hola {user.Nombre},

            Recibimos una solicitud para restablecer su contraseña en SIPITEX.
            Use este enlace (válido por 1 hora, de un solo uso):

            {link}

            Si usted no solicitó este cambio, ignore este mensaje.
            """;

        await _emailSender.SendAsync(
            user.Email,
            user.Nombre,
            "SIPITEX — Restablecer contraseña",
            body,
            cancellationToken);
    }

    public async Task<ServiceResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var passwordError = PasswordRules.Validate(newPassword, required: true);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return ServiceResult.Fail("Enlace inválido o expirado.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult.Fail("Enlace inválido o expirado.");

        var now = DateTime.UtcNow;
        var hash = HashToken(token);
        var resetToken = await _tokenRepository.FindValidAsync(user.Id, hash, now, cancellationToken);
        if (resetToken is null)
            return ServiceResult.Fail("Enlace inválido o expirado.");

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        _userRepository.Update(user);

        resetToken.UsedAtUtc = now;
        _tokenRepository.Update(resetToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Contraseña actualizada. Ya puede iniciar sesión.");
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string CreateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
