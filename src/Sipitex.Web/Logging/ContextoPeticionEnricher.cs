using System.Security.Claims;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace Sipitex.Web.Logging;

// Cada evento de la petición lleva el id de traza, el id numérico del usuario y el rol.
public sealed class ContextoPeticionEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _http;

    public ContextoPeticionEnricher(IHttpContextAccessor http) => _http = http;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var http = _http.HttpContext;
        if (http is null)
            return;

        logEvent.AddPropertyIfAbsent(factory.CreateProperty("RequestId", http.TraceIdentifier));

        var idRaw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idRaw, out var usuarioId) && usuarioId > 0)
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("UsuarioId", usuarioId));

        var rol = http.User.FindFirstValue(ClaimTypes.Role);
        if (!string.IsNullOrWhiteSpace(rol))
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("Rol", rol));
    }
}

// Empuja RequestId, el id numérico y el rol mientras dura la petición autenticada.
public sealed class ContextoPeticionMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var scopes = new List<IDisposable> { LogContext.PushProperty("RequestId", context.TraceIdentifier) };
        var idRaw = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idRaw, out var usuarioId) && usuarioId > 0)
            scopes.Add(LogContext.PushProperty("UsuarioId", usuarioId));

        var rol = context.User.FindFirstValue(ClaimTypes.Role);
        if (!string.IsNullOrWhiteSpace(rol))
            scopes.Add(LogContext.PushProperty("Rol", rol));

        try
        {
            await next(context);
        }
        finally
        {
            for (var i = scopes.Count - 1; i >= 0; i--)
                scopes[i].Dispose();
        }
    }
}
