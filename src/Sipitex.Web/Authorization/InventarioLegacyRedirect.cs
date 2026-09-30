using System.Security.Claims;
using Sipitex.Domain.Entities;

namespace Sipitex.Web.Authorization;

// Destino de /Inventario para favoritos viejos. Depende del rol y de la planta, así que la respuesta es 302.
public static class InventarioLegacyRedirect
{
    public const string Catalogo = "/PlantasInventario";

    public static string Destination(ClaimsPrincipal user, IReadOnlyList<int>? plantaIds)
    {
        if (user.Identity?.IsAuthenticated == true
            && user.IsInRole(UserRoles.EncargadoDeBodega)
            && !user.IsInRole(UserRoles.Administrador)
            && plantaIds is { Count: 1 }
            && plantaIds[0] > 0)
        {
            return $"/PlantasInventario/Detalle/{plantaIds[0]}";
        }

        return Catalogo;
    }
}
