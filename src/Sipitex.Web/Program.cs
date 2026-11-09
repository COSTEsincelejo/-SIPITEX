using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Formatting.Compact;
using Sipitex.Application.Seguridad;
using Microsoft.Extensions.Logging;
using Sipitex.Application.Ayuda;
using Sipitex.Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using Sipitex.Infrastructure;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Email;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Web;
using Sipitex.Web.Authorization;
using Sipitex.Web.Helpers;
using Sipitex.Web.Hosting;
using Sipitex.Web.Logging;
using Sipitex.Web.Security;

// Render inyecta PORT. Hay que fijar la URL antes de crear el host.
RenderListen.Apply();

var entornoInicial = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var consolaJson = string.Equals(entornoInicial, "Production", StringComparison.OrdinalIgnoreCase)
    || string.IsNullOrWhiteSpace(entornoInicial);
Log.Logger = consolaJson
    ? new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console(new CompactJsonFormatter()).CreateBootstrapLogger()
    : new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateBootstrapLogger();

// Punto de entrada de la web. Acá registro servicios y armo el pipeline HTTP.
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, logger) =>
    SipitexLogging.Configurar(logger, context.Configuration, context.HostingEnvironment, services));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<Serilog.Core.ILogEventEnricher, ContextoPeticionEnricher>();
builder.Services.AddSingleton<ContextoPeticionMiddleware>();

// Kestrel no anuncia el producto en la cabecera Server.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionStringsEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var rawConnection = PostgresConnectionStrings.SelectRaw(
    databaseUrl,
    connectionStringsEnv,
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Environment.IsProduction(),
    RunningUnderTestHost());
if (rawConnection is null)
{
    const string missingDb =
        "Falta la cadena de PostgreSQL. Configure DATABASE_URL o ConnectionStrings__DefaultConnection. " +
        "No se intenta una base local.";
    AbortarArranque(PasosArranque.CadenaConexion, missingDb);
    if (RunningUnderTestHost())
        throw new InvalidOperationException(missingDb);

    Environment.ExitCode = 1;
    return;
}

var cameFromEnvironment = !string.IsNullOrWhiteSpace(databaseUrl)
    || (!string.IsNullOrWhiteSpace(connectionStringsEnv)
        && string.Equals(rawConnection, connectionStringsEnv.Trim(), StringComparison.Ordinal));
if (cameFromEnvironment)
{
    try
    {
        builder.Configuration["ConnectionStrings:DefaultConnection"] = PostgresConnectionStrings.Normalize(rawConnection);
    }
    catch (Exception ex)
    {
        AbortarArranque(PasosArranque.CadenaConexion, "La cadena de PostgreSQL no se pudo leer.", ex);
        if (RunningUnderTestHost())
            throw;

        Environment.ExitCode = 1;
        return;
    }
}

// Render termina TLS y reenvía X-Forwarded-Proto. Sin esto el host puede redirigir a https://localhost.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// MVC: controladores + vistas. El filtro global exige antiforgery en POST/PUT/PATCH/DELETE.
builder.Services.AddControllersWithViews(mvc =>
{
    mvc.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.Configure<SoporteOptions>(builder.Configuration.GetSection(SoporteOptions.Seccion));
builder.Services.Configure<SeguridadOptions>(builder.Configuration.GetSection(SeguridadOptions.Seccion));

var seguridad = new SeguridadOptions();
builder.Configuration.GetSection(SeguridadOptions.Seccion).Bind(seguridad);
var cookieSegura = builder.Environment.IsDevelopment() || RunningUnderTestHost()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
// Servicios de negocio de la capa Application (inventario, MRP, usuarios, etc.)
builder.Services.AddApplicationServices();
// BD, repositorios y cosas de infraestructura
builder.Services.AddInfrastructure(builder.Configuration);

// Las claves de las cookies viven en DataProtectionKeys. Un deploy de Render no cierra sesiones.
builder.Services.AddDataProtection()
    .SetApplicationName("SIPITEX")
    .PersistKeysToDbContext<SipitexDbContext>();

// Login con cookies, no JWT ni nada raro.
// En el host de pruebas (HTTP) SameAsRequest aunque el entorno diga Production: si no, el cliente no devuelve la cookie.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(seguridad.MinutosEfectivos());
        options.SlidingExpiration = true;
        options.Cookie.Name = "sipitex.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = cookieSegura;
        options.Events.OnRedirectToLogin = context =>
        {
            if (SesionAutenticada.PideJson(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "sipitex.antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = cookieSegura;
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
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

var emailOptions = app.Services.GetRequiredService<IOptions<EmailOptions>>().Value;
var (emailLevel, emailDetail) = EmailConfiguration.StartupStatus(emailOptions);
app.Logger.Log(emailLevel, "Configuración de correo: {Detail}", emailDetail);

var privacidad = app.Services.GetRequiredService<IOptions<PrivacidadOptions>>().Value;
foreach (var aviso in PrivacidadConfiguracion.Advertencias(privacidad))
    app.Logger.LogWarning("{Aviso}", aviso);

// Migraciones y semilla. Si fallan, se registra el paso y el proceso termina. No se reintenta.
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
    var seedDemoUsers = ResolveSeedDemoData(app);
    var adminSeedPassword = app.Configuration["ADMIN_SEED_PASSWORD"];
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    await DbInitializer.InitializeAsync(db, seedDemoUsers, adminSeedPassword, logger);
}
catch (Exception ex)
{
    var paso = ex is ArranqueException arranque ? arranque.Paso : PasosArranque.Inicializacion;
    RegistroSeguro.Fatal(app.Logger, ex, paso);
    Console.Error.WriteLine("SIPITEX: arranque abortado (exit 1). Paso " + paso);
    if (RunningUnderTestHost())
        throw;

    Environment.ExitCode = 1;
    return;
}

// Render pone el esquema real en X-Forwarded-Proto. Tiene que ir antes de HSTS y la redirección.
app.UseForwardedHeaders();

// En producción no mostramos el stack trace feo al usuario
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var codigo = CodigoCorrelacion.Crear();
        context.Items[CodigoCorrelacion.Clave] = codigo;
        var errores = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Sipitex.Errores");
        var idRaw = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? usuarioId = int.TryParse(idRaw, out var id) && id > 0 ? id : null;
        var rol = context.User.FindFirstValue(ClaimTypes.Role);
        errores.LogError(
            ex,
            "Error no controlado {CodigoError} {RequestId} {UsuarioId} {Rol}",
            codigo,
            context.TraceIdentifier,
            usuarioId,
            string.IsNullOrWhiteSpace(rol) ? null : rol);
        throw;
    }
});

app.UseMiddleware<SecurityHeadersMiddleware>();

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
app.UseMiddleware<ContextoPeticionMiddleware>();
app.UseSerilogRequestLogging(SipitexLogging.ConfigurarPeticion);
app.UseMiddleware<AceptacionPoliticaMiddleware>();

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
if (RunningUnderTestHost())
{
    app.MapGet("/pruebas/error", () =>
    {
        throw new InvalidOperationException("fallo-controlado-prueba");
    }).AllowAnonymous();
}
app.MapControllers(); // rutas por atributo (ej. /api/busqueda)
// La raíz abre el panel. El inventario vive en PlantasInventario/Detalle/{id}.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run(); // levanta el servidor

static void AbortarArranque(string paso, string detalle, Exception? ex = null)
{
    if (ex is null)
    {
        Log.Fatal("Arranque abortado en el paso {Paso}. No se reintenta. {Detalle}", paso, detalle);
    }
    else
    {
        var texto = ex.ToString();
        if (RegistroSeguro.TieneSecreto(texto))
            Log.Fatal("Arranque abortado en el paso {Paso}. No se reintenta. {Detalle}", paso, RegistroSeguro.Redactar(texto));
        else
            Log.Fatal(ex, "Arranque abortado en el paso {Paso}. No se reintenta.", paso);
    }

    Console.Error.WriteLine("SIPITEX: arranque abortado (exit 1). Paso " + paso);
    Log.CloseAndFlush();
}

static bool ResolveSeedDemoData(WebApplication app)
{
    var raw = Environment.GetEnvironmentVariable("SEED_DEMO_DATA");
    if (!string.IsNullOrWhiteSpace(raw))
    {
        return raw.Trim() is "1" or "true" or "TRUE" or "True" or "yes" or "YES" or "Yes";
    }

    return app.Configuration.GetValue("Seed:DemoUsers", app.Environment.IsDevelopment());
}

static bool RunningUnderTestHost() =>
    AppDomain.CurrentDomain.GetAssemblies().Any(static assembly =>
        string.Equals(assembly.GetName().Name, "Microsoft.AspNetCore.Mvc.Testing", StringComparison.Ordinal));

// Lo pide el proyecto de tests de integración para levantar la app
public partial class Program;
