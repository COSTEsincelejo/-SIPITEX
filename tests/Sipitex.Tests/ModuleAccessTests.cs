using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class ModuleAccessTests
{
    private readonly SipitexWebAppFactory _factory;

    public ModuleAccessTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Encargado_MenuTypoSingular_ReturnsNotFound_PluralOk()
    {
        var client = await LoginAsync("bodega@sipitex.test", "Bodega123!");

        var typo = await client.GetAsync("/PlantaInventarioSolicitudes");
        Assert.Equal(HttpStatusCode.NotFound, typo.StatusCode);

        var typoOrdenes = await client.GetAsync("/PlantaInventarioOrdenes");
        Assert.Equal(HttpStatusCode.NotFound, typoOrdenes.StatusCode);

        var ok = await client.GetAsync("/PlantasInventarioSolicitudes");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var html = await ok.Content.ReadAsStringAsync();
        Assert.Contains("Solicitudes de materiales", html, StringComparison.OrdinalIgnoreCase);

        var ordenes = await client.GetAsync("/PlantasInventarioOrdenes");
        Assert.Equal(HttpStatusCode.OK, ordenes.StatusCode);

        var reingreso = await client.GetAsync("/PlantasInventarioOrdenes/Reingreso");
        Assert.Equal(HttpStatusCode.OK, reingreso.StatusCode);

        var movimientos = await client.GetAsync("/Inventario/Movimientos");
        Assert.Equal(HttpStatusCode.OK, movimientos.StatusCode);

        var actas = await client.GetAsync("/Actas");
        Assert.Equal(HttpStatusCode.OK, actas.StatusCode);
        var actasHtml = await actas.Content.ReadAsStringAsync();
        Assert.Contains("Nueva acta", actasHtml, StringComparison.OrdinalIgnoreCase);

        var create = await client.GetAsync("/Actas/Create");
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var createHtml = await create.Content.ReadAsStringAsync();
        Assert.Contains("Registrar acta", createHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Origen Manual", createHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Administrador_AccedeALosCincoModulos()
    {
        var client = await LoginAsync("admin@sipitex.test", "Admin123!");

        foreach (var path in new[]
                 {
                     "/PlantasInventarioSolicitudes",
                     "/PlantasInventarioOrdenes",
                     "/PlantasInventarioOrdenes/Reingreso",
                     "/Inventario/Movimientos",
                     "/Actas"
                 })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Instructor_VeSolicitudesYOrdenes_NoReingreso()
    {
        var client = await LoginAsync("instructor@sipitex.test", "Instructor123!");

        var solicitudes = await client.GetAsync("/PlantasInventarioSolicitudes");
        Assert.Equal(HttpStatusCode.OK, solicitudes.StatusCode);

        var ordenes = await client.GetAsync("/PlantasInventarioOrdenes");
        Assert.Equal(HttpStatusCode.OK, ordenes.StatusCode);

        var reingreso = await client.GetAsync("/PlantasInventarioOrdenes/Reingreso");
        Assert.Equal(HttpStatusCode.Redirect, reingreso.StatusCode);
        Assert.Contains("/Account/AccessDenied", reingreso.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var actas = await client.GetAsync("/Actas");
        Assert.Equal(HttpStatusCode.OK, actas.StatusCode);
    }

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = ExtractAntiforgery(html);

        var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        return client;
    }

    private static string ExtractAntiforgery(string html)
    {
        var match = Regex.Match(
            html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            match = Regex.Match(
                html,
                @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase);
        }

        Assert.True(match.Success, "No se encontró el token antiforgery en el login.");
        return match.Groups[1].Value;
    }
}
