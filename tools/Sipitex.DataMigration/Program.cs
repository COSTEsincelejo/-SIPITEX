using Sipitex.DataMigration;
using Sipitex.Infrastructure.Persistence;

// Herramienta manual. No la llama el arranque de SIPITEX.
if (args.Any(a => a is "-h" or "--help"))
{
    PrintUsage();
    return 0;
}

var dryRun = args.Any(a => a == "--dry-run");
if (args.Any(a => a != "--dry-run"))
{
    Console.Error.WriteLine("Argumento no reconocido. Use --dry-run o --help.");
    PrintUsage();
    return 1;
}

var sourceRaw = Environment.GetEnvironmentVariable("SOURCE_CONNECTION");
var destinationRaw = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(sourceRaw) || string.IsNullOrWhiteSpace(destinationRaw))
{
    Console.Error.WriteLine("Defina SOURCE_CONNECTION (origen) y DATABASE_URL (destino PostgreSQL).");
    PrintUsage();
    return 1;
}

try
{
    var source = SourceConnection.Open(sourceRaw);
    var destinationCs = PostgresConnectionStrings.Normalize(destinationRaw);
    using (source)
    {
        var secrets = new[]
        {
            source.Password,
            TryPassword(destinationCs)
        };
        try
        {
            return Migrator.Run(source, destinationCs, dryRun);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("La migración falló: " + Sanitize(ex.Message, secrets));
            return 1;
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("No se pudo abrir el origen o el destino: " + Sanitize(ex.Message, [TryPassword(sourceRaw), TryPassword(destinationRaw)]));
    return 1;
}

static string? TryPassword(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return null;
    try
    {
        if (raw.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = raw.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
                ? PostgresConnectionStrings.Normalize(raw)
                : raw;
            var password = new Npgsql.NpgsqlConnectionStringBuilder(normalized).Password;
            return string.IsNullOrEmpty(password) ? null : password;
        }
    }
    catch (ArgumentException)
    {
        return null;
    }

    return null;
}

static string Sanitize(string message, IEnumerable<string?> secrets)
{
    foreach (var secret in secrets)
    {
        if (!string.IsNullOrEmpty(secret))
            message = message.Replace(secret, "***", StringComparison.Ordinal);
    }

    return message;
}

static void PrintUsage()
{
    Console.WriteLine(
        """
        Sipitex.DataMigration

        Copia todas las tablas de SOURCE_CONNECTION hacia DATABASE_URL.
        Conserva los Id, no duplica filas si se ejecuta dos veces y no corre al arrancar la app.

          SOURCE_CONNECTION   SQLite (Data Source=archivo.db) o PostgreSQL (URL o cadena Npgsql)
          DATABASE_URL        PostgreSQL de destino (postgresql://… o cadena Npgsql)

          --dry-run            solo imprime conteos; no escribe
          --help               esta ayuda

        Ejemplo local (cadena Npgsql, sin forzar SSL):

          export SOURCE_CONNECTION='Host=127.0.0.1;Port=5432;Database=sipitex;Username=sipitex;Password=…'
          export DATABASE_URL='Host=127.0.0.1;Port=5433;Database=sipitex;Username=sipitex;Password=…;SSL Mode=Disable'
          dotnet run --project tools/Sipitex.DataMigration -- --dry-run
          dotnet run --project tools/Sipitex.DataMigration
        """);
}
