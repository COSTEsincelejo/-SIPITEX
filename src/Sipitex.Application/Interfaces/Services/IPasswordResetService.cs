using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IPasswordResetService
{
    // No revela si el correo existe. requestIp alimenta el rate limit de envío.
    Task RequestResetAsync(string email, string? requestIp = null, CancellationToken cancellationToken = default);

    Task<ServiceResult> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default);
}
