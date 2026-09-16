using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

/// <summary>
/// Un solo host WAF para health + login HTTP. Cada factory usa una base Postgres aislada.
/// </summary>
[CollectionDefinition(WebAppCollection.Name, DisableParallelization = true)]
public sealed class WebAppCollection : ICollectionFixture<SipitexWebAppFactory>
{
    public const string Name = "SipitexWebApp";
}

public class SipitexWebAppFactory : WebApplicationFactory<Program>, IDisposable
{
    private string? _databaseName;

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

    private string EnsureDatabase()
    {
        PostgresDefaults.EnableCompatibilitySwitches();
        return _databaseName ??= PostgresTestSupport.CreateDatabase();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        var connectionString = PostgresTestSupport.BuildConnectionString(EnsureDatabase());
        builder.UseSetting(
            Microsoft.AspNetCore.Hosting.WebHostDefaults.EnvironmentKey,
            EnvironmentName);
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("Email:Enabled", "false");
        ApplyExtraSettings(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
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
        if (_databaseName is not null)
        {
            PostgresTestSupport.DropDatabase(_databaseName);
            _databaseName = null;
        }
    }
}

/// <summary>
/// Host aislado con Environment=Production: no siembra usuarios @sipitex.test.
/// Base Postgres propia para no compartir BD con el fixture de Development.
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
