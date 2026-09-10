namespace Sipitex.Application.Interfaces.Services;

public interface IAuditActorAccessor
{
    int? UserId { get; }
    string? UserName { get; }
}
