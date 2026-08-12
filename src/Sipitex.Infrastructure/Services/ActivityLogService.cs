using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Services;

// Persistencia de ActivityLog (capa Infrastructure; interfaz en Application)
public class ActivityLogService : IActivityLogService
{
    private readonly SipitexDbContext _db;
    private readonly IUserRepository _users;

    public ActivityLogService(SipitexDbContext db, IUserRepository users)
    {
        _db = db;
        _users = users;
    }

    public async Task LogAsync(
        int userId,
        string action,
        string entity,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return;
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(entity))
            return;

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        var userName = !string.IsNullOrWhiteSpace(user?.Nombre)
            ? user!.Nombre.Trim()
            : $"#{userId}";

        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = userId,
            UserName = userName.Length > 120 ? userName[..120] : userName,
            Action = action.Trim(),
            Entity = entity.Trim(),
            EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim(),
            Timestamp = DateTime.UtcNow,
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim()
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
