using Npgsql;

namespace Sipitex.Infrastructure.Persistence;

/// <summary>
/// Render entrega <c>postgres://</c> o <c>postgresql://</c>. Npgsql solo abre cadenas <c>Host=...;Password=...</c>.
/// Además, el modo GSS por defecto (Prefer) carga libgssapi; la imagen aspnet no la trae y el proceso
/// muere con SIGSEGV (exit 139) en la primera consulta.
/// </summary>
public static class PostgresConnectionStrings
{
    /// <summary>
    /// DATABASE_URL es la variable de producción. ConnectionStrings__DefaultConnection sigue valiendo
    /// si no apunta a la base local. En producción no se usa el 127.0.0.1 de appsettings.
    /// </summary>
    public static string? SelectRaw(
        string? databaseUrl,
        string? connectionStringsEnv,
        string? configured,
        bool production,
        bool underTest)
    {
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return databaseUrl.Trim();

        var fromEnv = string.IsNullOrWhiteSpace(connectionStringsEnv) ? null : connectionStringsEnv.Trim();
        var fromConfig = string.IsNullOrWhiteSpace(configured) ? null : configured.Trim();

        // El host de pruebas inyecta su propia base. No la pisa un localhost del entorno.
        if (underTest)
            return fromConfig ?? fromEnv;

        if (production)
            return fromEnv is not null && !IsLoopback(fromEnv) ? fromEnv : null;

        return fromEnv ?? fromConfig;
    }

    public static string Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la cadena de PostgreSQL. Configure la variable de entorno DATABASE_URL o ConnectionStrings__DefaultConnection.");
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
                "DATABASE_URL parece una URL de PostgreSQL, pero no se pudo leer el host. " +
                "Use postgresql://usuario:clave@host/base?sslmode=require o una cadena Npgsql Host=...;Database=...;Username=...;Password=...");
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
        builder.SslMode = SslMode.Require;
        return builder.ConnectionString;
    }

    private static bool IsLoopback(string raw)
    {
        if (IsPostgresUrl(raw.Trim()))
        {
            return Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri)
                && IsLoopbackHost(uri.Host);
        }

        try
        {
            return IsLoopbackHost(new NpgsqlConnectionStringBuilder(raw).Host);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsLoopbackHost(string? host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);

}
