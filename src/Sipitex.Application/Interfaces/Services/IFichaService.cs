using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Fichas de producción y registro de sesiones diarias
public interface IFichaService
{
    Task<IReadOnlyList<FichaDto>> GetFichasAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionSessionDto>> GetRecentSessionsAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> RegisterSessionAsync(
        RegisterProductionDto dto,
        int? registeredByUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> QuickRegisterAsync(
        int fichaId,
        int units,
        string? observations = null,
        int? registeredByUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> CreateFichaAsync(
        CreateFichaDto dto,
        int? instructorUserId = null,
        CancellationToken cancellationToken = default);
}
