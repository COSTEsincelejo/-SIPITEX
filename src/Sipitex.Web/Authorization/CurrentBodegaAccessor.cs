using System.Security.Claims;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Web.Authorization;

// Lee BodegaId del claim emitido en el login. Sin IUserRepository para no ciclar con el DbContext.
public sealed class CurrentBodegaAccessor : ICurrentBodegaAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentBodegaAccessor(IHttpContextAccessor http) => _http = http;

    public int? BodegaId
    {
        get
        {
            var user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            if (!user.IsInRole(UserRoles.Bodeguero))
                return null;

            var raw = user.FindFirstValue(BodegaClaimTypes.BodegaId);
            if (int.TryParse(raw, out var id) && id > 0)
                return id;

            // Bodeguero sin claim/bodega: filtro vacío, no “ver todas”.
            return 0;
        }
    }
}
