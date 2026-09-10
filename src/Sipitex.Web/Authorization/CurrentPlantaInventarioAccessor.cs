using System.Security.Claims;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Web.Authorization;

// Lee plantaInventario_id (0..N claims) emitidos en el login. Sin IUserRepository para no ciclar con el DbContext.
// El filtro no cambia hasta el próximo login (intencional: la cookie es la fuente).
public sealed class CurrentPlantaInventarioAccessor : ICurrentPlantaInventarioAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentPlantaInventarioAccessor(IHttpContextAccessor http) => _http = http;

    public IReadOnlyList<int>? PlantaInventarioIds
    {
        get
        {
            var user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            if (!user.IsInRole(UserRoles.EncargadoDeBodega))
                return null;

            return user.FindAll(PlantaInventarioClaimTypes.PlantaInventarioId)
                .Select(c => int.TryParse(c.Value, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToArray();
        }
    }
}
