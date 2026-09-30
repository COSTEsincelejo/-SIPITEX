using Npgsql;

namespace Sipitex.Infrastructure.Persistence;

/// <summary>
/// Render entrega <c>postgres://</c> o <c>postgresql://</c>. Npgsql solo abre cadenas <c>Host=...;Password=...</c>.
/// Además, el modo GSS por defecto (Prefer) carga libgssapi; la imagen aspnet no la trae y el proceso
/// muere con SIGSEGV (exit 139) en la primera consulta.
/// </summary>
public static class PostgresConnectionStrings
{
    public static string Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta ConnectionStrings:DefaultConnection (variable de entorno ConnectionStrings__DefaultConnection).");
        }

        var trimmed = connectionString.Trim();
        if (IsPostgresUrl(trimmed))
            trimmed = FromRenderUrl(trimmed);

        var builder = new NpgsqlConnectionStringBuilder(trimmed)
        {
            GssEncryptionMode = GssEncryptionMode.Disable
        };
        return builder.ConnectionString;
    }

    private static bool IsPostgresUrl(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static string FromRenderUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection parece una URL de Render, pero no se pudo leer el host. " +
                "Use postgres://usuario:clave@host:5432/base o una cadena Npgsql Host=...;Database=...;Username=...;Password=...");
        }

        var user = "";
        var password = "";
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            user = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
                password = Uri.UnescapeDataString(parts[1]);
        }

        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (string.IsNullOrEmpty(database))
        {
            throw new InvalidOperationException(
                "La URL de PostgreSQL no incluye el nombre de la base.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = database,
            Username = user,
            Password = password
        };
        ApplySslMode(builder, uri.Query);
        return builder.ConnectionString;
    }

    private static void ApplySslMode(NpgsqlConnectionStringBuilder builder, string query)
    {
        if (string.IsNullOrEmpty(query))
            return;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var key = Uri.UnescapeDataString(eq >= 0 ? pair[..eq] : pair);
            if (!key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = eq >= 0 ? Uri.UnescapeDataString(pair[(eq + 1)..]) : "";
            builder.SslMode = value.ToLowerInvariant() switch
            {
                "disable" => SslMode.Disable,
                "allow" => SslMode.Allow,
                "prefer" => SslMode.Prefer,
                "require" => SslMode.Require,
                "verify-ca" or "verifyca" => SslMode.VerifyCA,
                "verify-full" or "verifyfull" => SslMode.VerifyFull,
                _ => throw new InvalidOperationException(
                    $"sslmode '{value}' en la URL de PostgreSQL no es un valor que Npgsql reconozca.")
            };
        }
    }
}
