using System.Security.Claims;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Authorization;

// Quién puede hacer qué según rol + permisos extra en claims
// El admin siempre pasa; instructor puede tener permisos puntuales
public static class PermissionRules
{
    // Admin, bodeguero o instructor con permiso extendido de inventario
    public static bool PuedeRegistrarMateriales(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && HasPermission(user, ExtendedPermissions.InventarioRegistrar));

    // Aprobar solicitudes: mismos roles base + permiso SolicitudesAprobar
    public static bool PuedeAprobarSolicitudes(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && HasPermission(user, ExtendedPermissions.SolicitudesAprobar));

    // Simular MRP: admin, bodeguero o instructor con MrpSimular
    public static bool PuedeSimularMrp(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && HasPermission(user, ExtendedPermissions.MrpSimular));

    // Crear/editar fichas técnicas: admin, bodeguero o instructor con Mrp.GestionarFichas (gap #6)
    // Delete permanece solo Admin en MrpController.
    public static bool PuedeGestionarFichasTecnicas(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || user.IsInRole(UserRoles.Bodeguero)
        || (user.IsInRole(UserRoles.Instructor) && HasPermission(user, ExtendedPermissions.MrpGestionarFichas));

    // Alertas: admin o cualquier rol con claim AlertasConfigurar
    public static bool PuedeConfigurarAlertas(ClaimsPrincipal user) =>
        user.IsInRole(UserRoles.Administrador)
        || HasPermission(user, ExtendedPermissions.AlertasConfigurar);

    // Revisa si el usuario tiene un permiso extendido en sus claims
    public static bool HasPermission(ClaimsPrincipal user, string permission) =>
        user.HasClaim(ExtendedPermissions.ClaimType, permission);
}
