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
    public void UrlSinSslMode_QuedaEnSslRequire()
    {
        var normalized = PostgresConnectionStrings.Normalize(
            "postgresql://usuario:clave@dpg-abc-a/sipitex");

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        Assert.Equal("dpg-abc-a", builder.Host);
        Assert.Equal("sipitex", builder.Database);
        Assert.Equal("usuario", builder.Username);
        Assert.Equal("clave", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal(GssEncryptionMode.Disable, builder.GssEncryptionMode);
    }

    [Fact]
    public void CadenaNpgsql_NoFuerzaSsl()
    {
        var builder = new NpgsqlConnectionStringBuilder(PostgresConnectionStrings.Normalize(
            "Host=db.interno;Database=sipitex;Username=sipitex;Password=secreta"));
        Assert.Equal(SslMode.Prefer, builder.SslMode);
    }

    [Fact]
    public void SelectRaw_DatabaseUrlTienePrioridad()
    {
        var selected = PostgresConnectionStrings.SelectRaw(
            "postgresql://usuario:clave@db.render.com/sipitex",
            "Host=localhost;Database=sipitex;Username=sipitex;Password=sipitex",
            "Host=127.0.0.1;Database=otra",
            production: true,
            underTest: false);

        Assert.StartsWith("postgresql://", selected, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectRaw_ProduccionSinVariable_NoUsaLocalhost()
    {
        Assert.Null(PostgresConnectionStrings.SelectRaw(
            null,
            "Host=127.0.0.1;Port=5432;Database=sipitex;Username=sipitex;Password=sipitex",
            "Host=localhost;Database=sipitex;Username=sipitex;Password=sipitex",
            production: true,
            underTest: false));

        Assert.Null(PostgresConnectionStrings.SelectRaw(
            "  ",
            null,
            "Host=localhost;Database=sipitex",
            production: true,
            underTest: false));
    }

    [Fact]
    public void SelectRaw_Produccion_AceptaLaClaveAnteriorSiNoEsLoopback()
    {
        var selected = PostgresConnectionStrings.SelectRaw(
            null,
            "Host=dpg-abc-a;Database=sipitex;Username=sipitex;Password=secreta",
            "Host=127.0.0.1;Database=sipitex",
            production: true,
            underTest: false);

        Assert.Contains("dpg-abc-a", selected, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectRaw_EnPruebas_ConservaLaCadenaDelHost()
    {
        var selected = PostgresConnectionStrings.SelectRaw(
            null,
            "Host=127.0.0.1;Database=sipitex;Username=sipitex;Password=sipitex",
            "Host=localhost;Database=sipitex_t_abc;Username=sipitex;Password=sipitex",
            production: true,
            underTest: true);

        Assert.Contains("sipitex_t_abc", selected, StringComparison.Ordinal);
    }

    [Fact]
    public void UrlVacia_FallaConMensajeClaro()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PostgresConnectionStrings.Normalize("  "));
        Assert.Contains("DATABASE_URL", ex.Message, StringComparison.Ordinal);
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
