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
        Assert.Equal(HttpStatusCode.MovedPermanently, movimientos.StatusCode);
        var movimientosUrl = movimientos.Headers.Location?.IsAbsoluteUri == true
            ? movimientos.Headers.Location.PathAndQuery
            : movimientos.Headers.Location?.ToString() ?? "";
        Assert.Contains("/PlantasInventario/Movimientos", movimientosUrl, StringComparison.OrdinalIgnoreCase);
        var movimientosNuevo = await client.GetAsync(movimientosUrl);
        Assert.Equal(HttpStatusCode.OK, movimientosNuevo.StatusCode);

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
                     "/PlantasInventario/Movimientos",
                     "/Actas",
                     "/Trazabilidad",
                     "/PlantasInventario"
                 })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var consultar = await client.GetAsync("/PlantasInventario/Consultar?plantaInventarioId=1&q=tela&nivel=Critico");
        Assert.Equal(HttpStatusCode.Redirect, consultar.StatusCode);
        var consultarUrl = consultar.Headers.Location?.IsAbsoluteUri == true
            ? consultar.Headers.Location.PathAndQuery
            : consultar.Headers.Location?.ToString() ?? "";
        Assert.Contains("/PlantasInventario", consultarUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plantaId=1", consultarUrl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Consultar", consultarUrl, StringComparison.OrdinalIgnoreCase);

        var planta1 = await client.GetAsync("/PlantasInventario?plantaId=1");
        Assert.Equal(HttpStatusCode.OK, planta1.StatusCode);
        var planta1Html = await planta1.Content.ReadAsStringAsync();
        Assert.Contains("<h2>Inventario por planta</h2>", planta1Html, StringComparison.Ordinal);
        Assert.Contains("Tela Jersey", planta1Html, StringComparison.Ordinal);
        Assert.Contains("Costo promedio ponderado", planta1Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nivel de stock", planta1Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-open-gestion", planta1Html, StringComparison.Ordinal);
        Assert.Contains("Mostrar inactivas", planta1Html, StringComparison.Ordinal);

        var planta2 = await client.GetAsync("/PlantasInventario?plantaId=2");
        Assert.Equal(HttpStatusCode.OK, planta2.StatusCode);
        var planta2Html = await planta2.Content.ReadAsStringAsync();
        Assert.Contains("Sin insumos en esta planta", planta2Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tela Jersey", planta2Html, StringComparison.Ordinal);

        var criticos = await client.GetAsync("/PlantasInventario?plantaId=1&nivel=Critico");
        Assert.Equal(HttpStatusCode.OK, criticos.StatusCode);
        var criticosHtml = await criticos.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Tela Jersey", criticosHtml, StringComparison.Ordinal);
        Assert.Contains("Crítico", criticosHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InventarioLegacy_Redirige301_YElMenuNoMuestraInventario()
    {
        var admin = await LoginAsync("admin@sipitex.test", "Admin123!");
        var adminLegacy = await admin.GetAsync("/Inventario");
        Assert.Equal(HttpStatusCode.MovedPermanently, adminLegacy.StatusCode);
        Assert.Contains("/PlantasInventario", adminLegacy.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/PlantasInventario/Detalle/", adminLegacy.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var catalogo = await admin.GetAsync("/PlantasInventario");
        Assert.Equal(HttpStatusCode.OK, catalogo.StatusCode);
        var catalogoHtml = await catalogo.Content.ReadAsStringAsync();
        Assert.Contains("<h2>Inventario por planta</h2>", catalogoHtml, StringComparison.Ordinal);
        Assert.Contains("Inventario por planta</a>", catalogoHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/Inventario\"", catalogoHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/Inventario/Movimientos\"", catalogoHtml, StringComparison.Ordinal);
        Assert.Contains("Ver inventario", catalogoHtml, StringComparison.Ordinal);
        Assert.Contains("Administrar catálogo", catalogoHtml, StringComparison.Ordinal);

        var encargado = await LoginAsync("bodega@sipitex.test", "Bodega123!");
        var encargadoLegacy = await encargado.GetAsync("/Inventario");
        Assert.Equal(HttpStatusCode.MovedPermanently, encargadoLegacy.StatusCode);
        Assert.Contains("/PlantasInventario/Detalle/1", encargadoLegacy.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var menuEncargado = await encargado.GetAsync("/PlantasInventario/Consultar");
        Assert.Equal(HttpStatusCode.Redirect, menuEncargado.StatusCode);
        var menuUrl = menuEncargado.Headers.Location?.IsAbsoluteUri == true
            ? menuEncargado.Headers.Location.PathAndQuery
            : menuEncargado.Headers.Location?.ToString() ?? "";
        var menuPagina = await encargado.GetAsync(menuUrl);
        Assert.Equal(HttpStatusCode.OK, menuPagina.StatusCode);
        var menuHtml = await menuPagina.Content.ReadAsStringAsync();
        Assert.DoesNotContain("href=\"/Inventario\"", menuHtml, StringComparison.Ordinal);
        Assert.Contains("Inventario por planta</a>", menuHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Inventario por bodega</a>", menuHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Administrador_CatalogoPlantas_MuestraBotonVer()
    {
        var client = await LoginAsync("admin@sipitex.test", "Admin123!");

        var response = await client.GetAsync("/PlantasInventario");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("fa-eye", html, StringComparison.Ordinal);
        Assert.Contains("Ver inventario", html, StringComparison.Ordinal);
        Assert.Contains("> Ver</button>", html, StringComparison.Ordinal);
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
        Assert.Equal(HttpStatusCode.Redirect, consultar.StatusCode);

        var catalogo = await client.GetAsync("/PlantasInventario");
        Assert.Equal(HttpStatusCode.OK, catalogo.StatusCode);
        var consultarHtml = await catalogo.Content.ReadAsStringAsync();
        Assert.Contains("<h2>Inventario por planta</h2>", consultarHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("data-open-gestion", consultarHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Mostrar inactivas", consultarHtml, StringComparison.Ordinal);
        Assert.DoesNotContain(">Reactivar<", consultarHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Nueva planta de inventario", consultarHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Editar<", consultarHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Eliminar<", consultarHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Desactivar<", consultarHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Encargado_ConsultaPlantaAjena_NoVeEsaPlantaNiSuInventario()
    {
        var client = await LoginAsync("bodega@sipitex.test", "Bodega123!");

        var consultar = await client.GetAsync("/PlantasInventario");
        Assert.Equal(HttpStatusCode.OK, consultar.StatusCode);
        var html = await consultar.Content.ReadAsStringAsync();
        Assert.Contains("<h2>Inventario por planta</h2>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Planta de Inventario 2", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-open-gestion", html, StringComparison.Ordinal);

        var ajenaRedirect = await client.GetAsync("/PlantasInventario/Consultar?plantaInventarioId=2");
        Assert.Equal(HttpStatusCode.Redirect, ajenaRedirect.StatusCode);
        var ajena = await client.GetAsync("/PlantasInventario?plantaId=2");
        Assert.Equal(HttpStatusCode.OK, ajena.StatusCode);
        var ajenaHtml = await ajena.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Planta de Inventario 2", ajenaHtml, StringComparison.Ordinal);
        Assert.Contains("Planta de Inventario 1", ajenaHtml, StringComparison.Ordinal);

        var propia = await client.GetAsync("/PlantasInventario?plantaId=1");
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
        await AceptarPoliticaSiHaceFaltaAsync(client);
        return client;
    }

    private static async Task AceptarPoliticaSiHaceFaltaAsync(HttpClient client)
    {
        var pagina = await client.GetAsync("/Privacidad/Aceptar");
        if (pagina.StatusCode != HttpStatusCode.OK)
            return;

        var token = ExtractAntiforgery(await pagina.Content.ReadAsStringAsync());
        var acepta = await client.PostAsync("/Privacidad/Aceptar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["AceptaPolitica"] = "true",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, acepta.StatusCode);
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
