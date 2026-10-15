namespace Sipitex.Web.Security;

// Cabeceras de endurecimiento. La CSP ya va en enforcement: los orígenes salen de lo que las vistas cargan hoy.
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = CspNonce.Crear();
        context.Items[CspNonce.ItemKey] = nonce;

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

            // En local HTTP no enviamos HSTS: el navegador se quedaría forzado a https://localhost.
            if (!_env.IsDevelopment() && context.Request.IsHttps)
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            headers["Content-Security-Policy"] = Politica(nonce);
            return Task.CompletedTask;
        });

        await _next(context);
    }

    // style-src lleva 'unsafe-inline' porque las vistas usan style="" en muchos campos.
    // script-src no: el nonce cubre los <script> de Razor y los manejadores inline se movieron a site.js.
    public static string Politica(string nonce) =>
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net 'nonce-" + nonce + "'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com data:; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self'";
}
