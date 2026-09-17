using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IEmailVerificationService
{
    Task<ServiceResult> RegisterAsync(string nombre, string email, string password, string? requestIp, CancellationToken cancellationToken = default);
    Task<ServiceResult> VerifyAsync(string email, string code, CancellationToken cancellationToken = default);
    Task<ServiceResult> ResendAsync(string email, string? requestIp, CancellationToken cancellationToken = default);
}
