using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class BuscadorLayoutTests
{
    private readonly SipitexWebAppFactory _factory;

    public BuscadorLayoutTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task PaginaAutenticada_NoMuestraAtajoNiRuedita_YSiElBotonBorrar()
    {
        var client = await LoginAsync("admin@sipitex.test", "Admin123!");
        var home = await client.GetAsync("/");
        home.EnsureSuccessStatusCode();
        var html = await home.Content.ReadAsStringAsync();

        Assert.DoesNotContain("search-kbd", html, StringComparison.Ordinal);
        Assert.DoesNotContain("search-spinner", html, StringComparison.Ordinal);
        Assert.DoesNotContain("search-loading", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Ctrl K<", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Borrar búsqueda\"", html, StringComparison.Ordinal);
        Assert.Contains("type=\"button\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-keyshortcuts=\"Control+K Slash\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"false\"", html, StringComparison.Ordinal);
        Assert.Contains("Buscar órdenes, materiales, fichas… (ej. tela jersey)", html, StringComparison.Ordinal);
        Assert.Contains("id=\"buscador-accesos\"", html, StringComparison.Ordinal);
        Assert.Contains("/PlantasInventario", html, StringComparison.Ordinal);
        Assert.Contains("Importar inventario", html, StringComparison.Ordinal);
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
