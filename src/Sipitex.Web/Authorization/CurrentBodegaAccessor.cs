using System.Security.Claims;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Web.Authorization;

// Lee bodega_id (0..N claims) emitidos en el login. Sin IUserRepository para no ciclar con el DbContext.
// El filtro no cambia hasta el próximo login (intencional: la cookie es la fuente).
public sealed class CurrentBodegaAccessor : ICurrentBodegaAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentBodegaAccessor(IHttpContextAccessor http) => _http = http;

    public IReadOnlyList<int>? BodegaIds
    {
        get
        {
            var user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            if (!user.IsInRole(UserRoles.Bodeguero))
                return null;

            return user.FindAll(BodegaClaimTypes.BodegaId)
                .Select(c => int.TryParse(c.Value, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToArray();
        }
    }
}
