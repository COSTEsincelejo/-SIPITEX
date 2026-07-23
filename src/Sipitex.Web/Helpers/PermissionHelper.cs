using System.Security.Claims;
using Sipitex.Domain.Entities;

namespace Sipitex.Web.Helpers;

public static class PermissionHelper
{
    public static bool HasPermission(ClaimsPrincipal user, string permission) =>
        user.HasClaim(UserPermissions.ClaimType, permission)
        || user.HasClaim(UserPermissions.ClaimType, UserPermissions.FuncionesAdministrador);

    public static bool CanRegisterMaterials(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && (
            user.HasClaim(UserPermissions.ClaimType, UserPermissions.RegistrarMateriales)
            || user.HasClaim(UserPermissions.ClaimType, UserPermissions.FuncionesAdministrador)));

    public static bool CanManageInventory(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && (
            user.HasClaim(UserPermissions.ClaimType, UserPermissions.GestionInventario)
            || user.HasClaim(UserPermissions.ClaimType, UserPermissions.FuncionesAdministrador)
            || user.HasClaim(UserPermissions.ClaimType, UserPermissions.AprobarSolicitudes)));

    public static IEnumerable<Claim> BuildPermissionClaims(string? permisosExtendidos) =>
        UserPermissions.Parse(permisosExtendidos)
            .Select(p => new Claim(UserPermissions.ClaimType, p));
}
