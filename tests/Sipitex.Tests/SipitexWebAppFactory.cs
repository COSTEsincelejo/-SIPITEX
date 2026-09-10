using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Sipitex.Tests;

/// <summary>
/// Un solo host WAF para health + login HTTP. Dos factories en paralelo
/// caían en el mismo sipitex.db (appsettings pisa el AddInMemoryCollection)
/// y SeedUsersAsync chocaba UNIQUE Users.Email.
/// </summary>
[CollectionDefinition(WebAppCollection.Name, DisableParallelization = true)]
public sealed class WebAppCollection : ICollectionFixture<SipitexWebAppFactory>
{
    public const string Name = "SipitexWebApp";
}

public sealed class SipitexWebAppFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"sipitex-webapp-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting(
            Microsoft.AspNetCore.Hosting.WebHostDefaults.EnvironmentKey,
            "Development");
        // UseSetting gana a appsettings.json; AddInMemoryCollection en ConfigureAppConfiguration no.
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Email:Enabled", "false");

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
