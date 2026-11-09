using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class OrdenesProductoComboboxTests
{
    private readonly SipitexWebAppFactory _factory;

    public OrdenesProductoComboboxTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Index_Administrador_UsaComboboxConProductosElegibles_SinDatalist()
    {
        var client = await LoginAsync("admin@sipitex.test", "Admin123!");
        var page = await client.GetAsync("/Ordenes");
        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync();

        Assert.DoesNotContain("<datalist", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("list=\"productos\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-combobox=\"productos-orden\"", html, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"off\"", html, StringComparison.Ordinal);
        Assert.Contains("data-combobox-create-url=\"/Mrp/Create\"", html, StringComparison.Ordinal);
        Assert.Contains("Crear ficha técnica", System.Net.WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.Contains("\"nombre\":\"Camisa\"", html, StringComparison.Ordinal);
        Assert.Contains("\"materiales\":3", html, StringComparison.Ordinal);
        Assert.Contains("name=\"CreateOrder.ProductName\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_ConProductoDeLaLista_CreaLaOrden()
    {
        var client = await LoginAsync("admin@sipitex.test", "Admin123!");
        var page = await client.GetAsync("/Ordenes");
        page.EnsureSuccessStatusCode();
        var token = ExtractAntiforgery(await page.Content.ReadAsStringAsync());
        var cliente = "Combo " + Guid.NewGuid().ToString("N");

        var post = await client.PostAsync("/Ordenes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CreateOrder.ProductName"] = "Camisa",
            ["CreateOrder.TotalQuantity"] = "4",
            ["CreateOrder.ClientName"] = cliente,
            ["CreateOrder.Deadline"] = DateOnly.FromDateTime(DateTime.Today.AddDays(10)).ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
        var orden = await db.ProductionOrders.SingleAsync(o => o.ClientName == cliente);
        Assert.Equal("Camisa", orden.ProductName);
        Assert.Equal(4, orden.TotalQuantity);
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
