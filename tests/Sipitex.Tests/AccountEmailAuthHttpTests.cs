using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class AccountEmailAuthHttpTests
{
    private readonly SipitexWebAppFactory _factory;

    public AccountEmailAuthHttpTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Get_IsAnonymous()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/Register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Crear cuenta", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("código", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ForgotPassword_Get_AsksForCodeNotLink()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/ForgotPassword");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("código de 6 dígitos", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("enlace de un solo uso", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyEmail_Get_IsAnonymous()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/VerifyEmail?email=ana@sipitex.test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Verificar correo", html, StringComparison.OrdinalIgnoreCase);
    }
}
