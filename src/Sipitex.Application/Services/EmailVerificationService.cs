using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthCodeIssuer _codes;
    private readonly IAuthCodeRequestGuard _ipGuard;

    public EmailVerificationService(
        IUserRepository users,
        IPasswordResetTokenRepository tokens,
        IUnitOfWork unitOfWork,
        IAuthCodeIssuer codes,
        IAuthCodeRequestGuard ipGuard)
    {
        _users = users;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _codes = codes;
        _ipGuard = ipGuard;
    }

    public async Task<ServiceResult> RegisterAsync(
        string nombre,
        string email,
        string password,
        string? requestIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return ServiceResult.Fail("El nombre es obligatorio.");
        if (!EmailFormat.IsValid(email))
            return ServiceResult.Fail("Correo no válido.");
        var passwordError = PasswordRules.Validate(password, required: true);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        if (_ipGuard.IsLimited(requestIp, AuthCodePurposes.EmailVerification))
            return ServiceResult.Fail(AuthCodeMessages.Cooldown);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await _users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            if (existing.EmailConfirmed)
                return ServiceResult.Fail("Ya existe un usuario con ese correo.");

            _ipGuard.Record(requestIp, AuthCodePurposes.EmailVerification);
            var resent = await IssueAsync(existing, cancellationToken);
            return resent.Success
                ? ServiceResult.Ok("Revisamos si enviamos un código a su correo.")
                : resent;
        }

        var user = new User
        {
            Nombre = nombre.Trim(),
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(password),
            Rol = UserRoles.Instructor,
            IsActive = true,
            EmailConfirmed = false
        };
        _users.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _ipGuard.Record(requestIp, AuthCodePurposes.EmailVerification);
        var issued = await IssueAsync(user, cancellationToken);
        return issued.Success
            ? ServiceResult.Ok("Cuenta creada. Ingrese el código enviado a su correo.")
            : issued;
    }

    public async Task<ServiceResult> VerifyAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!EmailFormat.IsValid(email))
            return ServiceResult.Fail("Correo no válido.");

        var user = await _users.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult.Fail(AuthCodeMessages.InvalidOrExpired);

        if (user.EmailConfirmed)
            return ServiceResult.Ok(AuthCodeMessages.EmailVerified);

        var consumed = await _codes.ConsumeAsync(user.Id, AuthCodePurposes.EmailVerification, code, cancellationToken);
        if (!consumed.Success)
            return consumed;

        user.EmailConfirmed = true;
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(AuthCodeMessages.EmailVerified);
    }

    public async Task<ServiceResult> ResendAsync(
        string email,
        string? requestIp,
        CancellationToken cancellationToken = default)
    {
        if (!EmailFormat.IsValid(email))
            return ServiceResult.Fail("Correo no válido.");
        if (_ipGuard.IsLimited(requestIp, AuthCodePurposes.EmailVerification))
            return ServiceResult.Fail(AuthCodeMessages.Cooldown);

        _ipGuard.Record(requestIp, AuthCodePurposes.EmailVerification);

        var user = await _users.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive || user.EmailConfirmed)
            return ServiceResult.Ok("Si el correo está pendiente de verificación, se envió un código.");

        var latest = await _tokens.FindLatestUnusedAsync(user.Id, AuthCodePurposes.EmailVerification, cancellationToken);
        if (latest is not null && latest.CreatedAtUtc.Add(AuthCodeHelper.ResendCooldown) > DateTime.UtcNow)
            return ServiceResult.Fail(AuthCodeMessages.Cooldown);

        var issued = await IssueAsync(user, cancellationToken);
        return issued.Success
            ? ServiceResult.Ok("Si el correo está pendiente de verificación, se envió un código.")
            : issued;
    }

    private Task<ServiceResult> IssueAsync(User user, CancellationToken cancellationToken) =>
        MapIssue(_codes.TryIssueAsync(
            user,
            AuthCodePurposes.EmailVerification,
            AuthCodeHelper.EmailVerificationLifetime,
            "SIPITEX — Verifique su correo",
            "Use este código para activar su cuenta en SIPITEX.",
            cancellationToken));

    private static async Task<ServiceResult> MapIssue(Task<AuthCodeIssueStatus> issue)
    {
        var status = await issue;
        return status switch
        {
            AuthCodeIssueStatus.Cooldown => ServiceResult.Fail(AuthCodeMessages.Cooldown),
            AuthCodeIssueStatus.RateLimited => ServiceResult.Fail(AuthCodeMessages.Cooldown),
            _ => ServiceResult.Ok()
        };
    }
}
