using Microsoft.Extensions.Configuration;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class PostgresConnectionTests
{
    [Fact]
    public void Normalize_HostEquals_SeDejaIgual()
    {
        const string npgsql = "Host=localhost;Port=5432;Database=sipitex;Username=sipitex;Password=sipitex";
        Assert.Equal(npgsql, PostgresConnection.Normalize(npgsql));
    }

    [Fact]
    public void Normalize_UriDeRender_ConvierteANpgsqlConSsl()
    {
        var normalized = PostgresConnection.Normalize(
            "postgres://sipitex:s3cret@dpg-abc.render.com:5432/sipitex");

        Assert.Contains("Host=dpg-abc.render.com", normalized, StringComparison.Ordinal);
        Assert.Contains("Port=5432", normalized, StringComparison.Ordinal);
        Assert.Contains("Database=sipitex", normalized, StringComparison.Ordinal);
        Assert.Contains("Username=sipitex", normalized, StringComparison.Ordinal);
        Assert.Contains("Password=s3cret", normalized, StringComparison.Ordinal);
        Assert.Contains("SSL Mode=Require", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_PrefiereConnectionStringYLuegoDatabaseUrl()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgres://u:p@db.example:5432/app"
            })
            .Build();

        var resolved = PostgresConnection.Resolve(config);
        Assert.Contains("Host=db.example", resolved, StringComparison.Ordinal);
        Assert.Contains("Username=u", resolved, StringComparison.Ordinal);
    }
}
