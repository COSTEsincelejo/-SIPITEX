namespace Sipitex.Application.Interfaces.Services;

// Auditoría global append-only (sin consulta UI en este PR)
public interface IActivityLogService
{
    Task LogAsync(
        int userId,
        string action,
        string entity,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}
