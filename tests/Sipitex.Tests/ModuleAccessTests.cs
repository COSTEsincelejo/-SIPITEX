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
                     "/Actas",
                     "/Trazabilidad",
                     "/PlantasInventario/Consultar"
                 })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var planta1 = await client.GetAsync("/PlantasInventario/Consultar?plantaInventarioId=1");
        Assert.Equal(HttpStatusCode.OK, planta1.StatusCode);
        var planta1Html = await planta1.Content.ReadAsStringAsync();
        Assert.Contains("Inventario por bodega", planta1Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tela Jersey", planta1Html, StringComparison.Ordinal);
        Assert.Contains("Costo promedio ponderado", planta1Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nivel de stock", planta1Html, StringComparison.OrdinalIgnoreCase);

        var planta2 = await client.GetAsync("/PlantasInventario/Consultar?plantaInventarioId=2");
        Assert.Equal(HttpStatusCode.OK, planta2.StatusCode);
        var planta2Html = await planta2.Content.ReadAsStringAsync();
        Assert.Contains("Sin insumos", planta2Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Esta bodega no tiene insumos registrados", planta2Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tela Jersey", planta2Html, StringComparison.Ordinal);

        var criticos = await client.GetAsync("/PlantasInventario/Consultar?plantaInventarioId=1&nivel=Critico");
        Assert.Equal(HttpStatusCode.OK, criticos.StatusCode);
        var criticosHtml = await criticos.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Tela Jersey", criticosHtml, StringComparison.Ordinal);
        Assert.Contains("Crítico", criticosHtml, StringComparison.Ordinal);
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

        var trazabilidad = await client.GetAsync("/Trazabilidad");
        Assert.Equal(HttpStatusCode.OK, trazabilidad.StatusCode);

        var consultar = await client.GetAsync("/PlantasInventario/Consultar");
        Assert.Equal(HttpStatusCode.OK, consultar.StatusCode);
        var consultarHtml = await consultar.Content.ReadAsStringAsync();
        Assert.Contains("Inventario por bodega", consultarHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nueva planta de inventario", consultarHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Editar<", consultarHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Eliminar<", consultarHtml, StringComparison.OrdinalIgnoreCase);

        var catalogo = await client.GetAsync("/PlantasInventario");
        Assert.Equal(HttpStatusCode.Redirect, catalogo.StatusCode);
        Assert.Contains("/Account/AccessDenied", catalogo.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Encargado_ConsultaPlantaAjena_NoVeEsaPlantaNiSuInventario()
    {
        var client = await LoginAsync("bodega@sipitex.test", "Bodega123!");

        var consultar = await client.GetAsync("/PlantasInventario/Consultar");
        Assert.Equal(HttpStatusCode.OK, consultar.StatusCode);
        var html = await consultar.Content.ReadAsStringAsync();
        Assert.Contains("Inventario por bodega", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Planta de Inventario 2", html, StringComparison.Ordinal);

        var ajena = await client.GetAsync("/PlantasInventario/Consultar?plantaInventarioId=2");
        Assert.Equal(HttpStatusCode.OK, ajena.StatusCode);
        var ajenaHtml = await ajena.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Planta de Inventario 2", ajenaHtml, StringComparison.Ordinal);
        Assert.Contains("Planta de Inventario 1", ajenaHtml, StringComparison.Ordinal);

        var propia = await client.GetAsync("/PlantasInventario/Consultar?plantaInventarioId=1");
        Assert.Equal(HttpStatusCode.OK, propia.StatusCode);
        var propiaHtml = await propia.Content.ReadAsStringAsync();
        Assert.Contains("Tela Jersey", propiaHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Planta de Inventario 2", propiaHtml, StringComparison.Ordinal);
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
