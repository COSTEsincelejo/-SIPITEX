using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Solicitud y confirmación de restablecimiento de contraseña
public interface IPasswordResetService
{
    // No revela si el correo existe; publicBaseUrl arma el link del mail
    Task RequestResetAsync(string email, string publicBaseUrl, CancellationToken cancellationToken = default);

    Task<ServiceResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
}
