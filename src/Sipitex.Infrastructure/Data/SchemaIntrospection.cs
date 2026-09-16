using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Data;

// Consultas de catálogo portables (SQLite en tests / PostgreSQL en runtime).
public static class SchemaIntrospection
{
    public static bool IsNpgsql(SipitexDbContext context) =>
        context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsSqlite(SipitexDbContext context) =>
        context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

    public static async Task<bool> TableExistsAsync(
        SipitexDbContext context,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        if (IsNpgsql(context))
        {
            command.CommandText =
                """
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = ANY (current_schemas(false))
                  AND table_name = @name
                LIMIT 1;
                """;
        }
        else
        {
            command.CommandText =
                "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @name LIMIT 1;";
        }

        AddParameter(command, "@name", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result is not DBNull;
    }

    public static async Task<bool> ColumnExistsAsync(
        SipitexDbContext context,
        string tableName,
        string columnName,
        CancellationToken cancellationToken = default)
    {
        if (!IsSafeIdentifier(tableName) || !IsSafeIdentifier(columnName))
            return false;

        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        if (IsNpgsql(context))
        {
            command.CommandText =
                """
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = ANY (current_schemas(false))
                  AND table_name = @table
                  AND column_name = @column
                LIMIT 1;
                """;
            AddParameter(command, "@table", tableName);
            AddParameter(command, "@column", columnName);
        }
        else
        {
            command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result is not DBNull;
    }

    public static async Task<bool> MigrationRowExistsAsync(
        SipitexDbContext context,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = @id LIMIT 1;""";
        AddParameter(command, "@id", migrationId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result is not DBNull;
    }

    public static bool IsSafeIdentifier(string name) =>
        name.Length > 0 && name.All(c => char.IsLetterOrDigit(c) || c == '_');

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
