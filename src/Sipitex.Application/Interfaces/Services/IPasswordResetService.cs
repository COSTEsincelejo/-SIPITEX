using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Services;

// Códigos de 6 dígitos: recuperar contraseña y confirmar el correo al crear la cuenta
public interface IPasswordResetService
{
    // No revela si el correo existe. El correo lleva solo el código, sin enlace.
    Task RequestResetAsync(string email, CancellationToken cancellationToken = default);

    Task<ServiceResult> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken = default);

    // Lo llama el alta de usuario (y un cambio de correo aún sin confirmar). No incluye el código en el resultado.
    Task<ServiceResult> SendEmailConfirmationAsync(User user, CancellationToken cancellationToken = default);

    Task<ServiceResult> ConfirmEmailAsync(string email, string code, CancellationToken cancellationToken = default);

    // Reenvío con cooldown. Si el correo no tiene una cuenta pendiente, la respuesta es genérica.
    Task<ServiceResult> ResendEmailConfirmationAsync(string email, CancellationToken cancellationToken = default);
}
