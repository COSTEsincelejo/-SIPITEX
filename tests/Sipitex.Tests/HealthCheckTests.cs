using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Sipitex.Tests;

public class HealthCheckTests : IClassFixture<HealthCheckTests.SipitexWebApplicationFactory>
{
    private readonly SipitexWebApplicationFactory _factory;

    public HealthCheckTests(SipitexWebApplicationFactory factory) => _factory = factory;

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

    public sealed class SipitexWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
    {
        private readonly string _dbPath = Path.Combine(
            Path.GetTempPath(),
            $"sipitex-health-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting(
                Microsoft.AspNetCore.Hosting.WebHostDefaults.EnvironmentKey,
                "Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                    ["Email:Enabled"] = "false"
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                if (File.Exists(_dbPath)) File.Delete(_dbPath);
                foreach (var suffix in new[] { "-shm", "-wal" })
                {
                    var side = _dbPath + suffix;
                    if (File.Exists(side)) File.Delete(side);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
