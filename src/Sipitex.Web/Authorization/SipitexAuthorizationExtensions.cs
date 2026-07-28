using Microsoft.AspNetCore.Authorization;
using Sipitex.Application.Authorization;

namespace Sipitex.Web.Authorization;

public static class SipitexAuthorizationExtensions
{
    public static AuthorizationOptions AddSipitexPolicies(this AuthorizationOptions options)
    {
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
