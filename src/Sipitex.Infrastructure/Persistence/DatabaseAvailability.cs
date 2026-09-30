using System.Net.Sockets;
using Npgsql;

namespace Sipitex.Infrastructure.Persistence;

public static class DatabaseAvailability
{
    public static bool IsStartupTransient(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or SocketException)
                return true;

            if (current is NpgsqlException npgsql && npgsql.IsTransient)
                return true;

            if (current is PostgresException postgres && postgres.SqlState is
                "08000" or "08001" or "08003" or "08006" or "57P01" or "57P03" or "53300")
                return true;
        }

        return false;
    }

    public static string Describe(Exception exception)
    {
        var parts = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
            parts.Add($"{current.GetType().Name}: {current.Message}");

        return string.Join(" | ", parts);
    }
}
