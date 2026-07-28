using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IPasswordResetService
{
    /// <summary>
    /// Solicita un enlace de reseteo. Nunca revela si el correo existe.
    /// <paramref name="publicBaseUrl"/> se usa solo para armar el enlace del correo (ej. https://host).
    /// </summary>
    Task RequestResetAsync(string email, string publicBaseUrl, CancellationToken cancellationToken = default);

    Task<ServiceResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
}
