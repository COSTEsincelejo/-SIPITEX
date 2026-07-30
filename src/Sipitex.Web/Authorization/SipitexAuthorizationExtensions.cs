using Microsoft.AspNetCore.Authorization;
using Sipitex.Application.Authorization;

namespace Sipitex.Web.Authorization;

// Conecta las reglas de permisos de Application con las políticas de ASP.NET
public static class SipitexAuthorizationExtensions
{
    public static AuthorizationOptions AddSipitexPolicies(this AuthorizationOptions options)
    {
        // Cada política se usa en controladores con [Authorize(Policy = "...")]

        // Quién puede dar de alta materiales en inventario
        options.AddPolicy(AuthorizationPolicyNames.PuedeRegistrarMateriales,
            policy => policy.RequireAssertion(ctx => PermissionRules.PuedeRegistrarMateriales(ctx.User)));

        // Quién puede aprobar o rechazar solicitudes de bodega
        options.AddPolicy(AuthorizationPolicyNames.PuedeAprobarSolicitudes,
            policy => policy.RequireAssertion(ctx => PermissionRules.PuedeAprobarSolicitudes(ctx.User)));

        // Quién puede correr la simulación MRP en la vista
        options.AddPolicy(AuthorizationPolicyNames.PuedeSimularMrp,
            policy => policy.RequireAssertion(ctx => PermissionRules.PuedeSimularMrp(ctx.User)));

        // Quién puede disparar la evaluación manual de alertas
        options.AddPolicy(AuthorizationPolicyNames.PuedeConfigurarAlertas,
            policy => policy.RequireAssertion(ctx => PermissionRules.PuedeConfigurarAlertas(ctx.User)));

        return options;
    }
}
