using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Services;

// Persistencia de ActivityLog (capa Infrastructure; interfaz en Application)
public class ActivityLogService : IActivityLogService
{
    private const int MaxRows = 250;

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

    public async Task<IReadOnlyList<ActivityLogDto>> QueryAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        string? action,
        string? entity,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ActivityLogs.AsNoTracking().AsQueryable();

        if (fromDate is DateOnly from)
        {
            var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(a => a.Timestamp >= fromUtc);
        }

        if (toDate is DateOnly to)
        {
            var toUtcExclusive = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(a => a.Timestamp < toUtcExclusive);
        }

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action.Trim());

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.Entity == entity.Trim());

        if (userId is > 0)
            query = query.Where(a => a.UserId == userId.Value);

        var rows = await query
            .OrderByDescending(a => a.Timestamp)
            .ThenByDescending(a => a.Id)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        return rows.Select(a => new ActivityLogDto(
            a.Id,
            a.Timestamp,
            a.UserId,
            a.UserName,
            a.Action,
            a.Entity,
            a.EntityId,
            a.Details)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken cancellationToken = default) =>
        await _db.ActivityLogs.AsNoTracking()
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetDistinctEntitiesAsync(CancellationToken cancellationToken = default) =>
        await _db.ActivityLogs.AsNoTracking()
            .Select(a => a.Entity)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ActivityLogActorDto>> GetDistinctActorsAsync(CancellationToken cancellationToken = default)
    {
        var pairs = await _db.ActivityLogs.AsNoTracking()
            .Select(a => new { a.UserId, a.UserName })
            .Distinct()
            .ToListAsync(cancellationToken);

        return pairs
            .GroupBy(p => p.UserId)
            .Select(g => new ActivityLogActorDto(g.Key, g.First().UserName))
            .OrderBy(a => a.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
