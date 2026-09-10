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

public class SipitexWebAppFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"sipitex-webapp-{Guid.NewGuid():N}.db");

    protected virtual string EnvironmentName => "Development";

    protected virtual IEnumerable<KeyValuePair<string, string?>> ExtraSettings =>
    [
        new("Seed:DemoUsers", "true")
    ];

    protected virtual void ApplyExtraSettings(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        foreach (var setting in ExtraSettings)
            builder.UseSetting(setting.Key, setting.Value);
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting(
            Microsoft.AspNetCore.Hosting.WebHostDefaults.EnvironmentKey,
            EnvironmentName);
        // UseSetting gana a appsettings.json; AddInMemoryCollection en ConfigureAppConfiguration no.
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Email:Enabled", "false");
        ApplyExtraSettings(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                ["Email:Enabled"] = "false"
            };
            foreach (var setting in ExtraSettings)
                values[setting.Key] = setting.Value;
            config.AddInMemoryCollection(values);
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

/// <summary>
/// Host aislado con Environment=Production: no siembra usuarios @sipitex.test.
/// SQLite propio para no compartir BD con el fixture de Development.
/// </summary>
public sealed class ProductionSeedWebAppFactory : SipitexWebAppFactory
{
    public const string AdminSeedPassword = "ProdAdmin123!";

    protected override string EnvironmentName => "Production";

    protected override IEnumerable<KeyValuePair<string, string?>> ExtraSettings =>
    [
        new("Seed:DemoUsers", "false"),
        new("ADMIN_SEED_PASSWORD", AdminSeedPassword)
    ];
}
