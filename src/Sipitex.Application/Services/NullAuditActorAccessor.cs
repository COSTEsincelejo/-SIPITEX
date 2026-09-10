using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

// Actor ausente (seed, migraciones, jobs). El interceptor no escribe ActivityLog.
public sealed class NullAuditActorAccessor : IAuditActorAccessor
{
    public int? UserId => null;
    public string? UserName => null;
}
