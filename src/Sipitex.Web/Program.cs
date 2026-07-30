using Microsoft.AspNetCore.Authentication.Cookies;
using Sipitex.Infrastructure;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Web;
using Sipitex.Web.Authorization;

// Punto de entrada de la web. Acá registro servicios y armo el pipeline HTTP.
var builder = WebApplication.CreateBuilder(args);

// MVC: controladores + vistas (lo típico en este proyecto)
builder.Services.AddControllersWithViews();
// Servicios de negocio de la capa Application
builder.Services.AddApplicationServices();
// BD, repositorios y cosas de infraestructura
builder.Services.AddInfrastructure(builder.Configuration);

// Login con cookies, no JWT ni nada raro
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // sesión de 8h
    });

// Políticas de permisos (quién puede hacer qué)
builder.Services.AddAuthorization(options => options.AddSipitexPolicies());

// Para saber si la BD responde (útil en despliegue)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SipitexDbContext>("database");

// Tarea en segundo plano que revisa alertas cada cierto tiempo
builder.Services.AddHostedService<Sipitex.Web.Hosting.AlertEvaluationHostedService>();

var app = builder.Build();

// Al arrancar, asegura que la BD tenga datos iniciales si hace falta
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// En producción no mostramos el stack trace feo al usuario
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // CSS, JS, imágenes de wwwroot
app.UseRouting();
app.UseAuthentication(); // tiene que ir antes de Authorization
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
// Ruta por defecto: al entrar va a Inventario
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inventario}/{action=Index}/{id?}");

app.Run();

// Lo pide el proyecto de tests de integración para levantar la app
public partial class Program;
