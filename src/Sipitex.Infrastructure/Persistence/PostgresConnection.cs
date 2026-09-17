using Microsoft.Extensions.Configuration;

namespace Sipitex.Infrastructure.Persistence;

// Resuelve la cadena de Postgres desde configuración (Render URI o formato Npgsql).
public static class PostgresConnection
{
    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var fromCs = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromCs))
            return Normalize(fromCs);

        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return Normalize(databaseUrl);

        return PostgresDefaults.LocalConnectionString;
    }

    // Acepta Host=… o postgres:// / postgresql:// (URL interna de Render).
    public static string Normalize(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return PostgresDefaults.LocalConnectionString;

        var raw = connectionString.Trim();
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return raw;

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            return raw;

        var user = Uri.UnescapeDataString(uri.UserInfo);
        var userName = user;
        var password = "";
        var colon = user.IndexOf(':');
        if (colon >= 0)
        {
            userName = user[..colon];
            password = user[(colon + 1)..];
        }

        var database = uri.AbsolutePath.Trim('/');
        var ssl = string.Equals(uri.Query.TrimStart('?'), "sslmode=require", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase);

        var builder = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={database};Username={userName};Password={password}";
        if (ssl || uri.Host.Contains("render.com", StringComparison.OrdinalIgnoreCase))
            builder += ";SSL Mode=Require;Trust Server Certificate=true";
        return builder;
    }
}
