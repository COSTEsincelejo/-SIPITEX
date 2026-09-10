using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Infrastructure.Persistence;

// Interceptor EF Core: registra altas/ediciones/bajas en ActivityLog sin que cada servicio lo recuerde.
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> Tracked = new(StringComparer.Ordinal)
    {
        nameof(Material),
        nameof(BomProduct),
        nameof(ProductionOrder),
        nameof(ConsumoMaterial),
        nameof(GrupoConfeccion),
        nameof(QualityRecord),
        nameof(ActaMovimiento)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IAuditActorAccessor _actor;

    public AuditSaveChangesInterceptor(IAuditActorAccessor actor) => _actor = actor;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AppendLogs(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendLogs(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void AppendLogs(DbContext? context)
    {
        if (context is null || _actor.UserId is not int userId)
            return;

        var userName = string.IsNullOrWhiteSpace(_actor.UserName) ? $"#{userId}" : _actor.UserName.Trim();
        if (userName.Length > 120)
            userName = userName[..120];

        var now = DateTime.UtcNow;
        var pending = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not ActivityLog && Tracked.Contains(e.Entity.GetType().Name))
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in pending)
        {
            var action = entry.State switch
            {
                EntityState.Added => ActivityLogActions.Create,
                EntityState.Deleted => ActivityLogActions.Delete,
                _ => ActivityLogActions.Update
            };
            var entity = entry.Entity.GetType().Name;
            var entityId = TryGetId(entry);
            var details = BuildDetails(entry);
            context.Set<ActivityLog>().Add(new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Timestamp = now,
                Details = details
            });
        }
    }

    private static string? TryGetId(EntityEntry entry)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
        var value = entry.State == EntityState.Added ? prop?.CurrentValue : (prop?.OriginalValue ?? prop?.CurrentValue);
        return value?.ToString();
    }

    private static string? BuildDetails(EntityEntry entry)
    {
        var payload = new Dictionary<string, object?> { ["state"] = entry.State.ToString() };
        if (entry.State == EntityState.Modified)
        {
            var before = new Dictionary<string, object?>();
            var after = new Dictionary<string, object?>();
            foreach (var p in entry.Properties.Where(p => p.IsModified && p.Metadata.Name != "Id"))
            {
                before[p.Metadata.Name] = p.OriginalValue;
                after[p.Metadata.Name] = p.CurrentValue;
            }
            payload["before"] = before;
            payload["after"] = after;
        }
        else if (entry.State == EntityState.Added)
        {
            payload["after"] = entry.Properties
                .Where(p => p.Metadata.Name != "Id")
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return json.Length > 2000 ? json[..2000] : json;
    }
}
