using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthCodeIssuer _codes;
    private readonly IAuthCodeRequestGuard _ipGuard;

    public PasswordResetService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuthCodeIssuer codes,
        IAuthCodeRequestGuard ipGuard)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _codes = codes;
        _ipGuard = ipGuard;
    }

    public async Task RequestResetAsync(string email, string? requestIp = null, CancellationToken cancellationToken = default)
    {
        if (!EmailFormat.IsValid(email))
            return;

        if (_ipGuard.IsLimited(requestIp, AuthCodePurposes.PasswordReset))
            return;

        _ipGuard.Record(requestIp, AuthCodePurposes.PasswordReset);

        var user = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive || !user.EmailConfirmed)
            return;

        await _codes.TryIssueAsync(
            user,
            AuthCodePurposes.PasswordReset,
            AuthCodeHelper.PasswordResetLifetime,
            "SIPITEX — Código para restablecer contraseña",
            "Use este código para restablecer su contraseña en SIPITEX.",
            cancellationToken);
    }

    public async Task<ServiceResult> ResetPasswordAsync(
        string email,
        string code,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var passwordError = PasswordRules.Validate(newPassword, required: true);
        if (passwordError is not null)
            return ServiceResult.Fail(passwordError);

        if (!EmailFormat.IsValid(email))
            return ServiceResult.Fail(AuthCodeMessages.InvalidOrExpired);

        var user = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive)
            return ServiceResult.Fail(AuthCodeMessages.InvalidOrExpired);

        var consumed = await _codes.ConsumeAsync(user.Id, AuthCodePurposes.PasswordReset, code, cancellationToken);
        if (!consumed.Success)
            return consumed;

        user.PasswordHash = PasswordHasher.Hash(newPassword);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(AuthCodeMessages.PasswordUpdated);
    }
}
