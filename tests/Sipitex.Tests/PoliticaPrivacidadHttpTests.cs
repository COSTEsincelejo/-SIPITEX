using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class PoliticaPrivacidadHttpTests
{
    private const string Instructor = "instructor@sipitex.test";
    private const string Clave = "Instructor123!";

    private readonly SipitexWebAppFactory _factory;

    public PoliticaPrivacidadHttpTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Politica_SinContacto_NoMuestraAppsettings()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var respuesta = await client.GetAsync("/Privacidad");
        respuesta.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        Assert.DoesNotContain("appsettings", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Pendiente de completar", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[COMPLETAR", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Correo de contacto:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Dirección de notificación:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Teléfono de contacto:", html, StringComparison.Ordinal);
        Assert.Contains("<h3>Introducción y alcance</h3>", html, StringComparison.Ordinal);
        Assert.Contains("<h3>Registro Nacional de Bases de Datos</h3>", html, StringComparison.Ordinal);
        Assert.Contains("1.0 (2026-10-15)", html, StringComparison.Ordinal);
        Assert.Contains("1.1 (2026-11-06)", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VersionNueva_ExigeAceptarOtraVez_YConservaLaAnterior()
    {
        await DejarSoloVersionAsync(Instructor, "1.0", "203.0.113.10");
        var client = await EntrarAsync(Instructor, Clave);

        var bloqueado = await client.GetAsync("/Ordenes");
        Assert.Equal(HttpStatusCode.Redirect, bloqueado.StatusCode);
        Assert.Contains("/Privacidad/Aceptar", bloqueado.Headers.Location?.OriginalString, StringComparison.Ordinal);

        var aviso = await client.GetAsync("/Privacidad/Aceptar");
        aviso.EnsureSuccessStatusCode();
        var html = await aviso.Content.ReadAsStringAsync();
        Assert.Contains("Usted aceptó la versión 1.0. La versión vigente es 1.1.", html, StringComparison.Ordinal);

        var token = ExtraerToken(html);
        var acepta = await client.PostAsync("/Privacidad/Aceptar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["AceptaPolitica"] = "true",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, acepta.StatusCode);
        Assert.DoesNotContain("/Privacidad/Aceptar", acepta.Headers.Location?.OriginalString, StringComparison.Ordinal);

        var abierto = await client.GetAsync("/Ordenes");
        Assert.Equal(HttpStatusCode.OK, abierto.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
        var userId = await db.Users.Where(u => u.Email == Instructor).Select(u => u.Id).SingleAsync();
        var versiones = await db.AceptacionesTratamiento
            .Where(a => a.UserId == userId)
            .Select(a => a.VersionPolitica)
            .ToListAsync();
        Assert.Contains("1.0", versiones);
        Assert.Contains("1.1", versiones);
    }

    [Fact]
    public async Task Aceptar_GuardaVersionFechaUtcEIp()
    {
        await BorrarAceptacionesAsync(Instructor);
        var antes = DateTime.UtcNow.AddMinutes(-2);
        var client = await EntrarAsync(Instructor, Clave);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.44");

        var pagina = await client.GetAsync("/Privacidad/Aceptar");
        pagina.EnsureSuccessStatusCode();
        var token = ExtraerToken(await pagina.Content.ReadAsStringAsync());
        var acepta = await client.PostAsync("/Privacidad/Aceptar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["AceptaPolitica"] = "true",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, acepta.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
        var userId = await db.Users.Where(u => u.Email == Instructor).Select(u => u.Id).SingleAsync();
        var fila = await db.AceptacionesTratamiento.SingleAsync(a => a.UserId == userId && a.VersionPolitica == "1.1");

        Assert.Equal("1.1", fila.VersionPolitica);
        Assert.Equal(DateTimeKind.Utc, fila.AceptadaEnUtc.Kind);
        Assert.InRange(fila.AceptadaEnUtc, antes, DateTime.UtcNow.AddMinutes(2));
        Assert.Equal("203.0.113.44", fila.DireccionIp);
    }

    [Fact]
    public async Task Casilla_NoVienePremarcada()
    {
        var anonimo = _factory.CreateClient();
        var confirmar = await anonimo.GetAsync("/Account/ConfirmEmail");
        confirmar.EnsureSuccessStatusCode();
        var casillaCorreo = EtiquetaCasilla(await confirmar.Content.ReadAsStringAsync(), "AceptaPolitica");
        Assert.Contains("type=\"checkbox\"", casillaCorreo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("checked", casillaCorreo, StringComparison.OrdinalIgnoreCase);

        await BorrarAceptacionesAsync(Instructor);
        var client = await EntrarAsync(Instructor, Clave);
        var aceptar = await client.GetAsync("/Privacidad/Aceptar");
        aceptar.EnsureSuccessStatusCode();
        var casilla = EtiquetaCasilla(await aceptar.Content.ReadAsStringAsync(), "AceptaPolitica");
        Assert.Contains("type=\"checkbox\"", casilla, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("checked", casilla, StringComparison.OrdinalIgnoreCase);
    }

    private async Task DejarSoloVersionAsync(string email, string version, string ip)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
        var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        var previas = await db.AceptacionesTratamiento.Where(a => a.UserId == userId).ToListAsync();
        db.AceptacionesTratamiento.RemoveRange(previas);
        db.AceptacionesTratamiento.Add(new AceptacionTratamiento
        {
            UserId = userId,
            VersionPolitica = version,
            AceptadaEnUtc = new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc),
            DireccionIp = ip
        });
        await db.SaveChangesAsync();
    }

    private async Task BorrarAceptacionesAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
        var userId = await db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        var previas = await db.AceptacionesTratamiento.Where(a => a.UserId == userId).ToListAsync();
        db.AceptacionesTratamiento.RemoveRange(previas);
        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> EntrarAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var login = await client.GetAsync("/Account/Login");
        login.EnsureSuccessStatusCode();
        var token = ExtraerToken(await login.Content.ReadAsStringAsync());
        var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        return client;
    }

    private static string ExtraerToken(string html)
    {
        var match = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(html, @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "No se encontró el token antiforgery.");
        return match.Groups[1].Value;
    }

    private static string EtiquetaCasilla(string html, string nombre)
    {
        foreach (Match match in Regex.Matches(html, "<input\\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var tag = match.Value;
            if (tag.Contains("type=\"checkbox\"", StringComparison.OrdinalIgnoreCase)
                && tag.Contains($"name=\"{nombre}\"", StringComparison.OrdinalIgnoreCase))
                return tag;
        }

        return "";
    }
}
