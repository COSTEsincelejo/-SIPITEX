using Microsoft.AspNetCore.Authorization;
using Sipitex.Application.Authorization;

namespace Sipitex.Web.Authorization;

// Conecta las reglas de permisos de Application con las políticas de ASP.NET
public static class SipitexAuthorizationExtensions
{
    public static AuthorizationOptions AddSipitexPolicies(this AuthorizationOptions options)
    {
        // Cada política se usa en controladores con [Authorize(Policy = "...")]
        options.AddPolicy(AuthorizationPolicyNames.PuedeRegistrarMateriales,
            policy => policy.RequireAssertion(ctx => PermissionRules.PuedeRegistrarMateriales(ctx.User)));

        options.AddPolicy(AuthorizationPolicyNames.PuedeAprobarSolicitudes,
            policy => policy.RequireAssertion(ctx => PermissionRules.PuedeAprobarSolicitudes(ctx.User)));

        options.AddPolicy(AuthorizationPolicyNames.PuedeSimularMrp,
            policy => policy.RequireAssertion(ctx => PermissionRules.PuedeSimularMrp(ctx.User)));

        options.AddPolicy(AuthorizationPolicyNames.PuedeConfigurarAlertas,
            policy => policy.RequireAssertion(ctx => PermissionRules.PuedeConfigurarAlertas(ctx.User)));

        return options;
    }
}
