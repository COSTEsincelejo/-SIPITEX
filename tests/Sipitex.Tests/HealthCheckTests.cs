using Microsoft.AspNetCore.Mvc.Testing;

namespace Sipitex.Tests;

[Collection(WebAppCollection.Name)]
public class HealthCheckTests
{
    private readonly SipitexWebAppFactory _factory;

    public HealthCheckTests(SipitexWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk_WhenDatabaseIsAvailable()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task Health_DoesNotRequireAuthentication()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/health");

        Assert.NotEqual(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
