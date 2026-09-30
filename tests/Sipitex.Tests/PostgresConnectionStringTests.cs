using System.Data;
using System.Net.Sockets;
using Npgsql;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class PostgresConnectionStringTests
{
    [Fact]
    public void CadenaNpgsql_DesactivaGssYConservaLosDatos()
    {
        var normalized = PostgresConnectionStrings.Normalize(
            "Host=db.internal;Port=5432;Database=sipitex;Username=sipitex;Password=secreta");

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        Assert.Equal("db.internal", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("sipitex", builder.Database);
        Assert.Equal("sipitex", builder.Username);
        Assert.Equal("secreta", builder.Password);
        Assert.Equal(GssEncryptionMode.Disable, builder.GssEncryptionMode);
    }

    [Fact]
    public void UrlDeRender_SinPuerto_Usa5432YDecodificaLaClave()
    {
        var normalized = PostgresConnectionStrings.Normalize(
            "postgresql://sipitex:p%40ss@dpg-abc-a/sipitex?sslmode=require");

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        Assert.Equal("dpg-abc-a", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("sipitex", builder.Database);
        Assert.Equal("sipitex", builder.Username);
        Assert.Equal("p@ss", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal(GssEncryptionMode.Disable, builder.GssEncryptionMode);
        Assert.DoesNotContain("postgres://", normalized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UrlVacia_FallaConMensajeClaro()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PostgresConnectionStrings.Normalize("  "));
        Assert.Contains("ConnectionStrings__DefaultConnection", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UrlDeRender_AbrePostgreSQLLocal()
    {
        var database = PostgresTestSupport.CreateDatabase();
        try
        {
            var source = new NpgsqlConnectionStringBuilder(PostgresTestSupport.BuildConnectionString(database));
            var url =
                $"postgres://{Uri.EscapeDataString(source.Username!)}:{Uri.EscapeDataString(source.Password!)}@{source.Host}:{source.Port}/{source.Database}";
            await using var connection = new NpgsqlConnection(PostgresConnectionStrings.Normalize(url));
            await connection.OpenAsync();
            Assert.Equal(ConnectionState.Open, connection.State);
        }
        finally
        {
            PostgresTestSupport.DropDatabase(database);
        }
    }

    [Fact]
    public void CaidaDeConexion_EsTransitoria_UnErrorDeEsquemaNo()
    {
        Assert.True(DatabaseAvailability.IsStartupTransient(new TimeoutException("timeout")));
        Assert.True(DatabaseAvailability.IsStartupTransient(new SocketException(111)));
        Assert.False(DatabaseAvailability.IsStartupTransient(
            new InvalidOperationException("el esquema no coincide con InitialCreate")));

        var detail = DatabaseAvailability.Describe(new InvalidOperationException("exterior", new TimeoutException("interior")));
        Assert.Contains("InvalidOperationException: exterior", detail, StringComparison.Ordinal);
        Assert.Contains("TimeoutException: interior", detail, StringComparison.Ordinal);
    }
}
