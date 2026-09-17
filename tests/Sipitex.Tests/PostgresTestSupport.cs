using Npgsql;

namespace Sipitex.Tests;

internal static class PostgresTestSupport
{
    internal static string AdminConnectionString => BuildConnectionString("postgres");

    internal static string BuildConnectionString(string database)
    {
        var host = Environment.GetEnvironmentVariable("SIPITEX_TEST_PG_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("SIPITEX_TEST_PG_PORT") ?? "5432";
        var user = Environment.GetEnvironmentVariable("SIPITEX_TEST_PG_USER") ?? "sipitex";
        var password = Environment.GetEnvironmentVariable("SIPITEX_TEST_PG_PASSWORD") ?? "sipitex";
        return $"Host={host};Port={port};Database={database};Username={user};Password={password};Include Error Detail=true";
    }

    internal static string CreateDatabase()
    {
        var name = $"sipitex_t_{Guid.NewGuid():N}";
        using var connection = new NpgsqlConnection(AdminConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{name}\";";
        command.ExecuteNonQuery();
        return name;
    }

    internal static void DropDatabase(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("sipitex_t_", StringComparison.Ordinal))
            return;

        try
        {
            using var connection = new NpgsqlConnection(AdminConnectionString);
            connection.Open();
            using (var terminate = connection.CreateCommand())
            {
                terminate.CommandText =
                    """
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = @name AND pid <> pg_backend_pid();
                    """;
                terminate.Parameters.AddWithValue("name", name);
                terminate.ExecuteNonQuery();
            }

            using var drop = connection.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{name}\";";
            drop.ExecuteNonQuery();
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
