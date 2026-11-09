using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class ActasCreateVistaTests
{
    private readonly SipitexWebAppFactory _factory;

    public ActasCreateVistaTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_OrigenDistintoDeEstadoProducto_NoRenderizaEstados()
    {
        var client = await LoginAsync("admin@sipitex.test", "Admin123!");
        var page = await client.GetAsync("/Actas/Create");
        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync();

        Assert.DoesNotContain("name=\"Form.EstadoOrigen\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"Form.EstadoDestino\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Estado origen", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Estado destino", html, StringComparison.Ordinal);
        Assert.Contains("Esta orden no tiene consumos", html, StringComparison.Ordinal);
        Assert.Contains("id=\"actaBuscar\"", html, StringComparison.Ordinal);
        Assert.Contains("Seleccionar todos", html, StringComparison.Ordinal);
        Assert.Contains("Firme aquí", html, StringComparison.Ordinal);

        var transicion = await client.GetAsync("/Actas/Create?origen=EstadoProducto");
        transicion.EnsureSuccessStatusCode();
        var conEstados = System.Net.WebUtility.HtmlDecode(await transicion.Content.ReadAsStringAsync());
        Assert.Contains("name=\"Form.EstadoOrigen\"", conEstados, StringComparison.Ordinal);
        Assert.Contains("name=\"Form.EstadoDestino\"", conEstados, StringComparison.Ordinal);
        Assert.Contains("Estado origen", conEstados, StringComparison.Ordinal);
        Assert.Contains("Estado destino", conEstados, StringComparison.Ordinal);
        Assert.Contains("Materia prima", conEstados, StringComparison.Ordinal);
        Assert.Contains("Confección", conEstados, StringComparison.Ordinal);
        Assert.Contains("Venta / entrega", conEstados, StringComparison.Ordinal);
    }

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var token = ExtractAntiforgery(await loginPage.Content.ReadAsStringAsync());

        var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);

        var aceptar = await client.GetAsync("/Privacidad/Aceptar");
        if (aceptar.StatusCode == HttpStatusCode.OK)
        {
            var tokenAceptar = ExtractAntiforgery(await aceptar.Content.ReadAsStringAsync());
            var acepta = await client.PostAsync("/Privacidad/Aceptar", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["AceptaPolitica"] = "true",
                ["__RequestVerificationToken"] = tokenAceptar
            }));
            Assert.Equal(HttpStatusCode.Redirect, acepta.StatusCode);
        }

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

        Assert.True(match.Success, "No se encontró el token antiforgery.");
        return match.Groups[1].Value;
    }
}
