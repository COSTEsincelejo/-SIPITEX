using Microsoft.EntityFrameworkCore;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Data;

/// <summary>
/// Marca <c>InitialCreate</c> como ya aplicada en BD legacy (EnsureCreated / SQL manual)
/// que ya tienen el esquema completo pero no tienen <c>__EFMigrationsHistory</c>.
/// Solo operaciones aditivas (CREATE / INSERT). Nunca DROP ni DELETE.
/// </summary>
public static class MigrationBaseline
{
    /// <summary>MigrationId exacto de <c>20260728214130_InitialCreate.cs</c>.</summary>
    public const string InitialCreateMigrationId = "20260728214130_InitialCreate";

    /// <summary>ProductVersion de EF Core usada al generar la migración.</summary>
    public const string EfProductVersion = "10.0.9";

    public static async Task EnsureBaselineAsync(
        SipitexDbContext context,
        CancellationToken cancellationToken = default)
    {
        // (a) Si ya hay historial de migraciones, no hacer nada.
        if (await TableExistsAsync(context, "__EFMigrationsHistory", cancellationToken))
            return;

        // (b)/(d) Sin Materials → BD nueva: MigrateAsync creará el esquema.
        if (!await TableExistsAsync(context, "Materials", cancellationToken))
            return;

        // (c) BD legacy con tablas. Solo baseline si el esquema parece el actual;
        // si falta algo que InitialCreate asume, NO ocultar el desfase.
        if (!await LooksLikeFullCurrentSchemaAsync(context, cancellationToken))
        {
            throw new InvalidOperationException(
                "sipitex.db tiene tablas de negocio pero el esquema no coincide con InitialCreate " +
                "(faltan columnas o tablas esperadas: p. ej. Materials.LastEntryDate, " +
                "QualityRecords.MotivoReproceso/Responsable, ProductionSessions, Users). " +
                "No se aplicó baseline automático. Haga backup (cp sipitex.db sipitex.db.bak) " +
                "y revise el desfase antes de continuar.");
        }

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """,
            cancellationToken);

        // Idempotente: no insertar si ya quedó registrada (p. ej. carrera o reintento).
        if (await MigrationRowExistsAsync(context, InitialCreateMigrationId, cancellationToken))
            return;

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({0}, {1});
            """,
            InitialCreateMigrationId,
            EfProductVersion);
    }

    private static async Task<bool> LooksLikeFullCurrentSchemaAsync(
        SipitexDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(context, "Materials", "LastEntryDate", cancellationToken))
            return false;
        if (!await TableExistsAsync(context, "QualityRecords", cancellationToken))
            return false;
        if (!await ColumnExistsAsync(context, "QualityRecords", "MotivoReproceso", cancellationToken))
            return false;
        if (!await ColumnExistsAsync(context, "QualityRecords", "Responsable", cancellationToken))
            return false;
        if (!await TableExistsAsync(context, "ProductionSessions", cancellationToken))
            return false;
        if (!await TableExistsAsync(context, "Users", cancellationToken))
            return false;
        return true;
    }

    internal static async Task<bool> TableExistsAsync(
        SipitexDbContext context,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result is not DBNull;
    }

    private static async Task<bool> ColumnExistsAsync(
        SipitexDbContext context,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // PRAGMA no acepta parámetros para el nombre de tabla; validamos identificador.
        if (!IsSafeSqliteIdentifier(tableName) || !IsSafeSqliteIdentifier(columnName))
            return false;
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static async Task<bool> MigrationRowExistsAsync(
        SipitexDbContext context,
        string migrationId,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = $id LIMIT 1;""";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$id";
        parameter.Value = migrationId;
        command.Parameters.Add(parameter);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result is not DBNull;
    }

    private static bool IsSafeSqliteIdentifier(string name) =>
        name.Length > 0 && name.All(c => char.IsLetterOrDigit(c) || c == '_');
}
