using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Sipitex.Web.Security;

namespace Sipitex.Tests;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Desarrollo_NoEnviaHsts_YLaCspNoPermiteScriptsInline()
    {
        using var server = new TestServer(Host("Development"));
        var respuesta = await server.CreateClient().GetAsync("/");

        Assert.False(respuesta.Headers.Contains("Strict-Transport-Security"));
        var csp = Cabecera(respuesta, "Content-Security-Policy");
        Assert.Contains("default-src 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("script-src 'self' https://cdn.jsdelivr.net", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp, StringComparison.Ordinal);
        Assert.Contains("style-src 'self' 'unsafe-inline'", csp, StringComparison.Ordinal);
        Assert.Equal("nosniff", Cabecera(respuesta, "X-Content-Type-Options"));
        Assert.Equal("DENY", Cabecera(respuesta, "X-Frame-Options"));
        Assert.Equal("strict-origin-when-cross-origin", Cabecera(respuesta, "Referrer-Policy"));
        Assert.Contains("camera=()", Cabecera(respuesta, "Permissions-Policy"), StringComparison.Ordinal);
        Assert.False(respuesta.Headers.Contains("Server"));
    }

    [Fact]
    public async Task ProduccionHttps_EnviaHstsUnAnio()
    {
        using var server = new TestServer(Host("Production", forzarHttps: true));
        var respuesta = await server.CreateClient().GetAsync("/");

        Assert.Equal(
            "max-age=31536000; includeSubDomains",
            Cabecera(respuesta, "Strict-Transport-Security"));
    }

    private static IWebHostBuilder Host(string entorno, bool forzarHttps = false) =>
        new WebHostBuilder()
            .UseEnvironment(entorno)
            .Configure(app =>
            {
                if (forzarHttps)
                {
                    app.Use((ctx, next) =>
                    {
                        ctx.Request.Scheme = "https";
                        return next();
                    });
                }

                app.UseMiddleware<SecurityHeadersMiddleware>();
                app.Run(async ctx =>
                {
                    ctx.Response.ContentType = "text/html";
                    await ctx.Response.WriteAsync("ok");
                });
            });

    private static string Cabecera(HttpResponseMessage respuesta, string nombre) =>
        respuesta.Headers.TryGetValues(nombre, out var valores)
            ? valores.First()
            : respuesta.Content.Headers.TryGetValues(nombre, out var delContenido)
                ? delContenido.First()
                : "";
}
