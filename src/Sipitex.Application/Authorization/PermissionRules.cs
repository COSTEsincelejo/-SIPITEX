using System.Security.Claims;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Authorization;

/// <summary>
/// Reglas de autorización usadas por las policies ASP.NET Core.
/// Administrador siempre pasa; el resto combina rol base + claim "permiso".
/// </summary>
public static class PermissionRules
{
    public static bool PuedeRegistrarMateriales(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && HasPermission(user, ExtendedPermissions.InventarioRegistrar));

    public static bool PuedeAprobarSolicitudes(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && HasPermission(user, ExtendedPermissions.SolicitudesAprobar));

    public static bool PuedeSimularMrp(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && HasPermission(user, ExtendedPermissions.MrpSimular));

    public static bool PuedeConfigurarAlertas(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || HasPermission(user, ExtendedPermissions.AlertasConfigurar);

    public static bool HasPermission(ClaimsPrincipal user, string permission) =>
        user.HasClaim(ExtendedPermissions.ClaimType, permission);
}
