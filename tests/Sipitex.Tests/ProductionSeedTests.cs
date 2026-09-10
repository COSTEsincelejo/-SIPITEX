using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class ProductionSeedTests : IDisposable
{
    private readonly ProductionSeedWebAppFactory _factory = new();

    [Fact]
    public async Task Production_DoesNotSeedDemoUsers_CreatesBootstrapAdmin()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SipitexDbContext>();
        var emails = await db.Users.Select(u => u.Email).ToListAsync();

        Assert.DoesNotContain(DbInitializer.DemoAdminEmail, emails, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(DbInitializer.DemoInstructorEmail, emails, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(DbInitializer.DemoEncargadoEmail, emails, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(DbInitializer.ProductionAdminEmail, emails, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(await db.Users.Where(u => u.Rol == "Administrador").Select(u => u.Email).ToListAsync(),
            e => string.Equals(e, DbInitializer.ProductionAdminEmail, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Production_LoginPage_HidesDemoCredentials()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var page = await client.GetAsync("/Account/Login");
        page.EnsureSuccessStatusCode();
        var html = System.Net.WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());

        Assert.DoesNotContain("Credenciales de demostración", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Admin123!", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Instructor123!", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Bodega123!", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_BootstrapAdmin_CanSignIn()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var token = ExtractAntiforgery(await loginPage.Content.ReadAsStringAsync());

        var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = DbInitializer.ProductionAdminEmail,
            ["Password"] = ProductionSeedWebAppFactory.AdminSeedPassword,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.DoesNotContain("/Account/Login", post.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _factory.Dispose();

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
                @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""",
                RegexOptions.IgnoreCase);
        }
        Assert.True(match.Success, "No se encontró el token antifalsificación.");
        return match.Groups[1].Value;
    }
}
