namespace Sipitex.Infrastructure.Persistence;

/// <summary>
/// Detecta si la cadena de conexión apunta a PostgreSQL o SQLite.
/// </summary>
public static class DatabaseProvider
{
    public const string Sqlite = "Sqlite";
    public const string PostgreSql = "PostgreSQL";

    public static string Resolve(string? connectionString, string? configuredProvider = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredProvider))
        {
            var normalized = configuredProvider.Trim();
            if (normalized.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
                return PostgreSql;

            if (normalized.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
                return Sqlite;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            return Sqlite;

        // Formatos típicos de Npgsql / PostgreSQL
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("User ID=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Port=5432", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase))
            return PostgreSql;

        return Sqlite;
    }

    public static bool IsPostgreSql(string? connectionString, string? configuredProvider = null) =>
        Resolve(connectionString, configuredProvider) == PostgreSql;
}
