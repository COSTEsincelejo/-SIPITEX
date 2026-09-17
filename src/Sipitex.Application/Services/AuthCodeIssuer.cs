using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class AuthCodeIssuer : IAuthCodeIssuer
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    public AuthCodeIssuer(
        IPasswordResetTokenRepository tokens,
        IUnitOfWork unitOfWork,
        IEmailSender emailSender)
    {
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }

    public async Task<AuthCodeIssueStatus> TryIssueAsync(
        User user,
        string purpose,
        TimeSpan lifetime,
        string subject,
        string instructions,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var recent = await _tokens.CountCreatedSinceAsync(
            user.Id, purpose, now - AuthCodeHelper.RateLimitWindow, cancellationToken);
        if (recent >= AuthCodeHelper.MaxRequestsPerWindow)
            return AuthCodeIssueStatus.RateLimited;

        var unused = await _tokens.GetUnusedByUserAsync(user.Id, purpose, cancellationToken);
        foreach (var previous in unused)
        {
            previous.UsedAtUtc = now;
            _tokens.Update(previous);
        }

        var plain = AuthCodeHelper.Generate();
        var entity = new PasswordResetToken
        {
            UserId = user.Id,
            Purpose = purpose,
            TokenHash = AuthCodeHelper.Hash(plain),
            ExpiresAtUtc = now.Add(lifetime),
            UsedAtUtc = null,
            CreatedAtUtc = now,
            FailedAttempts = 0
        };
        await _tokens.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var minutes = Math.Max(1, (int)Math.Round(lifetime.TotalMinutes));
        var body =
            $"""
            Hola {user.Nombre},

            {instructions}

            Código: {plain}

            Vence en {minutes} minutos y es de un solo uso.
            Si usted no solicitó este código, ignore este mensaje.
            """;

        await _emailSender.SendAsync(user.Email, user.Nombre, subject, body, cancellationToken);
        return AuthCodeIssueStatus.Issued;
    }

    public async Task<ServiceResult> ConsumeAsync(
        int userId,
        string purpose,
        string plainCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = AuthCodeHelper.Normalize(plainCode);
        if (normalized.Length != AuthCodeHelper.Length)
            return ServiceResult.Fail(AuthCodeMessages.InvalidOrExpired);

        var now = DateTime.UtcNow;
        var latest = await _tokens.FindLatestUnusedAsync(userId, purpose, cancellationToken);
        if (latest is null)
            return ServiceResult.Fail(AuthCodeMessages.InvalidOrExpired);

        if (latest.ExpiresAtUtc <= now)
        {
            latest.UsedAtUtc = now;
            _tokens.Update(latest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Fail(AuthCodeMessages.InvalidOrExpired);
        }

        if (!string.Equals(latest.TokenHash, AuthCodeHelper.Hash(normalized), StringComparison.Ordinal))
        {
            latest.FailedAttempts++;
            if (latest.FailedAttempts >= AuthCodeHelper.MaxFailedAttempts)
                latest.UsedAtUtc = now;
            _tokens.Update(latest);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult.Fail(
                latest.UsedAtUtc is not null
                    ? AuthCodeMessages.TooManyAttempts
                    : AuthCodeMessages.InvalidOrExpired);
        }

        latest.UsedAtUtc = now;
        _tokens.Update(latest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }
}
