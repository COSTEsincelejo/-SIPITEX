using System.Security.Claims;
using Microsoft.Extensions.Options;
using Sipitex.Application.Seguridad;

namespace Sipitex.Web.Security;

// Quien ya entró y no aceptó la versión vigente solo puede ver la política, salir o renovar la sesión.
public sealed class AceptacionPoliticaMiddleware
{
    private readonly RequestDelegate _next;

    public AceptacionPoliticaMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true && !Permitida(context.Request.Path))
        {
            var version = context.User.FindFirstValue(SesionAutenticada.PoliticaClaim);
            var vigente = context.RequestServices
                .GetRequiredService<IOptions<PrivacidadOptions>>()
                .Value.VersionVigente();

            if (!string.Equals(version, vigente, StringComparison.Ordinal))
            {
                context.Response.Redirect("/Privacidad/Aceptar");
                return;
            }
        }

        await _next(context);
    }

    private static bool Permitida(PathString path) =>
        path.StartsWithSegments("/Privacidad")
        || path.StartsWithSegments("/Account/Logout")
        || path.StartsWithSegments("/Account/KeepAlive")
        || path.StartsWithSegments("/Account/AccessDenied")
        || path.StartsWithSegments("/Account/Photo")
        || path.StartsWithSegments("/health")
        || path.StartsWithSegments("/healthz")
        || path.StartsWithSegments("/version");
}
