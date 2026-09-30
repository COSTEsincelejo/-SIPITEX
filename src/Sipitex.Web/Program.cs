using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Infrastructure;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Web;
using Sipitex.Web.Authorization;
using Sipitex.Web.Helpers;
using Sipitex.Web.Hosting;
using Sipitex.Web.Security;

// Render inyecta PORT. Hay que fijar la URL antes de crear el host.
RenderListen.Apply();

// Punto de entrada de la web. Acá registro servicios y armo el pipeline HTTP.
var builder = WebApplication.CreateBuilder(args);

// Render termina TLS y reenvía X-Forwarded-Proto. Sin esto el host puede redirigir a https://localhost.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// MVC: controladores + vistas (lo típico en este proyecto)
builder.Services.AddControllersWithViews();
// Servicios de negocio de la capa Application (inventario, MRP, usuarios, etc.)
builder.Services.AddApplicationServices();
// BD, repositorios y cosas de infraestructura
builder.Services.AddInfrastructure(builder.Configuration);

// Login con cookies, no JWT ni nada raro
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";           // si no está logueado, manda acá
        options.AccessDeniedPath = "/Account/AccessDenied"; // sin permiso para la acción
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // sesión de 8h
    });

// Políticas de permisos (quién puede hacer qué)
builder.Services.AddAuthorization(options => options.AddSipitexPolicies());

// Bloqueo de login por intentos fallidos (en memoria del proceso)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILoginAttemptGuard, MemoryLoginAttemptGuard>();

// Para saber si la BD responde (útil en despliegue)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SipitexDbContext>("database");

// Tarea en segundo plano que revisa alertas cada cierto tiempo
builder.Services.AddHostedService<Sipitex.Web.Hosting.AlertEvaluationHostedService>();

var app = builder.Build();

// Al arrancar, asegura que la BD tenga datos iniciales si hace falta.
// Si falla, se registra la causa y se relanza: un deploy roto no debe quedar a medias.
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
    var seedDemoUsers = app.Configuration.GetValue("Seed:DemoUsers", app.Environment.IsDevelopment());
    var adminSeedPassword = app.Configuration["ADMIN_SEED_PASSWORD"];
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    await DbInitializer.InitializeAsync(db, seedDemoUsers, adminSeedPassword, logger);
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "El arranque falló al preparar la base de datos. Revise ConnectionStrings__DefaultConnection y que PostgreSQL acepte conexiones. El proceso se detiene para que Render conserve la instancia anterior.");
    throw;
}

// En producción no mostramos el stack trace feo al usuario
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // página genérica de error
    app.UseHsts(); // fuerza HTTPS en el navegador
}

app.UseForwardedHeaders();

// Las páginas HTML no deben cachearse: un deploy nuevo tiene que verse sin hard-refresh.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var contentType = context.Response.ContentType;
        if (contentType is not null
            && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase)
            && !context.Response.Headers.ContainsKey("Cache-Control"))
        {
            context.Response.Headers.CacheControl = "no-cache, no-store";
        }

        return Task.CompletedTask;
    });
    await next();
});

// Las sondas de Render son HTTP. No redirigirlas a HTTPS.
app.UseWhen(
    context =>
    {
        var path = context.Request.Path;
        return !path.StartsWithSegments("/health")
            && !path.StartsWithSegments("/healthz")
            && !path.StartsWithSegments("/version");
    },
    branch => branch.UseHttpsRedirection());

app.UseStaticFiles(); // CSS, JS, imágenes de wwwroot
app.UseRouting(); // resuelve rutas antes de auth
app.UseAuthentication(); // tiene que ir antes de Authorization
app.UseAuthorization(); // revisa roles y políticas

app.MapGet("/healthz", () => Results.Text("ok", "text/plain")).AllowAnonymous();
app.MapGet("/version", (IWebHostEnvironment env) =>
{
    var raw = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT");
    var commit = string.IsNullOrWhiteSpace(raw) ? "local" : raw.Trim();
    var builtAt = Environment.GetEnvironmentVariable("SIPITEX_BUILD_DATE");
    if (string.IsNullOrWhiteSpace(builtAt))
    {
        var stampPath = Path.Combine(env.ContentRootPath, "build-timestamp.txt");
        if (File.Exists(stampPath))
            builtAt = File.ReadAllText(stampPath).Trim();
    }

    return Results.Json(new
    {
        commit,
        build = DisplayHelper.BuildLabel(commit == "local" ? "" : commit),
        builtAt = string.IsNullOrWhiteSpace(builtAt) ? null : builtAt
    });
}).AllowAnonymous();
app.MapHealthChecks("/health").AllowAnonymous(); // endpoint público de salud (consulta la BD)
app.MapControllers(); // rutas por atributo (ej. /api/busqueda)
// Ruta por defecto: al entrar va a Inventario
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inventario}/{action=Index}/{id?}");

app.Run(); // levanta el servidor

// Lo pide el proyecto de tests de integración para levantar la app
public partial class Program;
