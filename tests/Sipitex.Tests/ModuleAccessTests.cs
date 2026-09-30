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
        Assert.Equal(HttpStatusCode.Redirect, movimientos.StatusCode);
        var movimientosUrl = movimientos.Headers.Location?.IsAbsoluteUri == true
            ? movimientos.Headers.Location.PathAndQuery
            : movimientos.Headers.Location?.ToString() ?? "";
        Assert.Contains("/PlantasInventario/Detalle/1", movimientosUrl, StringComparison.OrdinalIgnoreCase);
        var movimientosNuevo = await client.GetAsync("/PlantasInventario/Movimientos");
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
                     "/PlantasInventario/Detalle/1"
                 })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var planta1 = await client.GetAsync("/PlantasInventario/Detalle/1");
        Assert.Equal(HttpStatusCode.OK, planta1.StatusCode);
        var planta1Html = await planta1.Content.ReadAsStringAsync();
        Assert.Contains("Inventario de", planta1Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tela Jersey", planta1Html, StringComparison.Ordinal);
        Assert.Contains("Costo promedio", planta1Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(">Nivel<", planta1Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Inventario por bodega", planta1Html, StringComparison.OrdinalIgnoreCase);

        var planta2 = await client.GetAsync("/PlantasInventario/Detalle/2");
        Assert.Equal(HttpStatusCode.OK, planta2.StatusCode);
        var planta2Html = await planta2.Content.ReadAsStringAsync();
        Assert.Contains("Sin inventario", planta2Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Esta planta no tiene materiales ni insumos registrados", planta2Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tela Jersey", planta2Html, StringComparison.Ordinal);

        var criticos = await client.GetAsync("/PlantasInventario/Detalle/1?nivel=Critico");
        Assert.Equal(HttpStatusCode.OK, criticos.StatusCode);
        var criticosHtml = await criticos.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Tela Jersey", criticosHtml, StringComparison.Ordinal);
        Assert.Contains("Crítico", criticosHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InventarioLegacy_Redirige302_YElMenuNoMuestraInventario()
    {
        var admin = await LoginAsync("admin@sipitex.test", "Admin123!");
        var adminLegacy = await admin.GetAsync("/Inventario");
        Assert.Equal(HttpStatusCode.Redirect, adminLegacy.StatusCode);
        Assert.Contains("/PlantasInventario", adminLegacy.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/PlantasInventario/Detalle/", adminLegacy.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var adminPost = await admin.PostAsync("/Inventario/AddMaterial", new StringContent(""));
        Assert.Equal(HttpStatusCode.Redirect, adminPost.StatusCode);
        Assert.DoesNotContain("/PlantasInventario/Detalle/", adminPost.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var despues = await admin.GetAsync("/PlantasInventario/Detalle/1");
        var htmlDespues = await despues.Content.ReadAsStringAsync();
        Assert.Contains("Tela Jersey", htmlDespues, StringComparison.Ordinal);
        Assert.DoesNotContain("data-toast-type=\"success\"", htmlDespues, StringComparison.Ordinal);

        var catalogo = await admin.GetAsync("/PlantasInventario");
        Assert.Equal(HttpStatusCode.OK, catalogo.StatusCode);
        var catalogoHtml = await catalogo.Content.ReadAsStringAsync();
        Assert.Contains("Plantas de inventario", catalogoHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/Inventario\"", catalogoHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/Inventario/Movimientos\"", catalogoHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("> Inventario</a>", catalogoHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Inventario por bodega", catalogoHtml, StringComparison.Ordinal);
        Assert.Contains("Consultar inventario</a>", catalogoHtml, StringComparison.Ordinal);

        var encargado = await LoginAsync("bodega@sipitex.test", "Bodega123!");
        var encargadoLegacy = await encargado.GetAsync("/Inventario");
        Assert.Equal(HttpStatusCode.Redirect, encargadoLegacy.StatusCode);
        Assert.Contains("/PlantasInventario/Detalle/1", encargadoLegacy.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var menuEncargado = await encargado.GetAsync("/PlantasInventario/Detalle/1");
        Assert.Equal(HttpStatusCode.OK, menuEncargado.StatusCode);
        var menuHtml = await menuEncargado.Content.ReadAsStringAsync();
        Assert.DoesNotContain("href=\"/Inventario\"", menuHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("> Inventario</a>", menuHtml, StringComparison.Ordinal);
        Assert.Contains("Plantas de inventario", menuHtml, StringComparison.Ordinal);

        var instructor = await LoginAsync("instructor@sipitex.test", "Instructor123!");
        var instructorLegacy = await instructor.GetAsync("/Inventario");
        Assert.Equal(HttpStatusCode.Redirect, instructorLegacy.StatusCode);
        var instructorPath = instructorLegacy.Headers.Location?.IsAbsoluteUri == true
            ? instructorLegacy.Headers.Location.AbsolutePath
            : instructorLegacy.Headers.Location?.ToString();
        Assert.Equal("/PlantasInventario", instructorPath);
    }

    [Fact]
    public async Task Administrador_CatalogoPlantas_MuestraBotonVer()
    {
        var client = await LoginAsync("admin@sipitex.test", "Admin123!");

        var response = await client.GetAsync("/PlantasInventario");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("fa-eye", html, StringComparison.Ordinal);
        Assert.Contains("Consultar inventario</a>", html, StringComparison.Ordinal);
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

        var catalogo = await client.GetAsync("/PlantasInventario");
        Assert.Equal(HttpStatusCode.OK, catalogo.StatusCode);
        var catalogoHtml = await catalogo.Content.ReadAsStringAsync();
        Assert.Contains("Plantas de inventario", catalogoHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Nueva planta de inventario", catalogoHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Editar<", catalogoHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Eliminar<", catalogoHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("> Inventario</a>", catalogoHtml, StringComparison.Ordinal);
        Assert.Contains("Consultar inventario</a>", catalogoHtml, StringComparison.Ordinal);

        var detalle = await client.GetAsync("/PlantasInventario/Detalle/1");
        Assert.Equal(HttpStatusCode.OK, detalle.StatusCode);
        var detalleHtml = await detalle.Content.ReadAsStringAsync();
        Assert.Contains("Tela Jersey", detalleHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Agregar material", detalleHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Ajustar<", detalleHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Eliminar<", detalleHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Historial de movimientos", detalleHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Encargado_ConsultaPlantaAjena_NoVeEsaPlantaNiSuInventario()
    {
        var client = await LoginAsync("bodega@sipitex.test", "Bodega123!");

        var ajena = await client.GetAsync("/PlantasInventario/Detalle/2");
        Assert.Equal(HttpStatusCode.Redirect, ajena.StatusCode);
        Assert.Contains("/Account/AccessDenied", ajena.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var propia = await client.GetAsync("/PlantasInventario/Detalle/1");
        Assert.Equal(HttpStatusCode.OK, propia.StatusCode);
        var propiaHtml = await propia.Content.ReadAsStringAsync();
        Assert.Contains("Tela Jersey", propiaHtml, StringComparison.Ordinal);
        Assert.Contains("Inventario de Planta de Inventario 1", propiaHtml, StringComparison.Ordinal);
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
