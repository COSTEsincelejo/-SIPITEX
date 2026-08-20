using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Auditoría global append-only + consulta para el panel Admin
public interface IActivityLogService
{
    Task LogAsync(
        int userId,
        string action,
        string entity,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityLogDto>> QueryAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? action,
        string? entity,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctEntitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityLogActorDto>> GetDistinctActorsAsync(CancellationToken cancellationToken = default);
}
