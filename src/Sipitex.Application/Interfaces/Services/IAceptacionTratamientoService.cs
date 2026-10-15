namespace Sipitex.Application.Interfaces.Services;

public sealed record AceptacionTratamientoDto(string VersionPolitica, DateTime AceptadaEnUtc, string? DireccionIp);

public interface IAceptacionTratamientoService
{
    // Versión vigente si la persona ya la aceptó; null si falta o es otra versión.
    Task<string?> VersionVigenteAsync(int userId, CancellationToken cancellationToken = default);

    Task<AceptacionTratamientoDto?> UltimaAsync(int userId, CancellationToken cancellationToken = default);

    Task RegistrarAsync(int userId, string? direccionIp, CancellationToken cancellationToken = default);

    Task RegistrarPorCorreoAsync(string email, string? direccionIp, CancellationToken cancellationToken = default);
}
