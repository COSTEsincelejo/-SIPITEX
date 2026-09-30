using System.Data.Common;
using Microsoft.Data.Sqlite;
using Npgsql;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.DataMigration;

sealed class SourceConnection : IDisposable
{
    private readonly DbConnection _connection;

    private SourceConnection(bool isSqlite, string description, string? password, DbConnection connection)
    {
        IsSqlite = isSqlite;
        Description = description;
        Password = password;
        _connection = connection;
    }

    public bool IsSqlite { get; }

    public string Description { get; }

    public string? Password { get; }

    public DbConnection Connection => _connection;

    public static SourceConnection Open(string raw)
    {
        var trimmed = raw.Trim();
        if (IsSqliteSource(trimmed))
        {
            var connectionString = NormalizeSqlite(trimmed);
            var builder = new SqliteConnectionStringBuilder(connectionString);
            var fileName = string.IsNullOrWhiteSpace(builder.DataSource)
                ? "memoria"
                : Path.GetFileName(builder.DataSource);
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return new SourceConnection(true, "SQLite (" + fileName + ")", null, connection);
        }

        var normalized = PostgresConnectionStrings.Normalize(trimmed);
        var pg = new NpgsqlConnectionStringBuilder(normalized)
        {
            IncludeErrorDetail = false
        };
        var npgsql = new NpgsqlConnection(pg.ConnectionString);
        npgsql.Open();
        return new SourceConnection(
            false,
            $"PostgreSQL host={pg.Host} base={pg.Database}",
            string.IsNullOrEmpty(pg.Password) ? null : pg.Password,
            npgsql);
    }

    public void Dispose() => _connection.Dispose();

    private static bool IsSqliteSource(string value)
    {
        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return false;

        if (value.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            return false;

        if (value.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Filename=", StringComparison.OrdinalIgnoreCase))
            return true;

        return value.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".sqlite3", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSqlite(string value)
    {
        if (value.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Filename=", StringComparison.OrdinalIgnoreCase))
            return value;

        return "Data Source=" + value;
    }
}
