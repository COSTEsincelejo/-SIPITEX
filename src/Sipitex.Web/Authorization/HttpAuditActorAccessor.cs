using System.Security.Claims;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Web.Authorization;

public sealed class HttpAuditActorAccessor : IAuditActorAccessor
{
    private readonly IHttpContextAccessor _http;

    public HttpAuditActorAccessor(IHttpContextAccessor http) => _http = http;

    public int? UserId
    {
        get
        {
            var raw = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) && id > 0 ? id : null;
        }
    }

    public string? UserName => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
}
