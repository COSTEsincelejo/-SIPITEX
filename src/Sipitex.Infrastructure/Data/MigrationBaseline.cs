using Microsoft.EntityFrameworkCore;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Data;

/// <summary>
/// Marca migraciones ya reflejadas en BD legacy (EnsureCreated / SQL manual)
/// que tienen tablas de negocio pero no <c>__EFMigrationsHistory</c>.
/// Solo operaciones aditivas (CREATE / INSERT). Nunca DROP ni DELETE.
/// </summary>
public static class MigrationBaseline
{
    /// <summary>MigrationId exacto de <c>20260728214130_InitialCreate.cs</c>.</summary>
    public const string InitialCreateMigrationId = "20260728214130_InitialCreate";

    /// <summary>MigrationId exacto de <c>20260728221016_AddPasswordResetTokens.cs</c>.</summary>
    public const string AddPasswordResetTokensMigrationId = "20260728221016_AddPasswordResetTokens";

    /// <summary>MigrationId exacto de <c>20260728234022_AddUserPhotoPath.cs</c>.</summary>
    public const string AddUserPhotoPathMigrationId = "20260728234022_AddUserPhotoPath";

    /// <summary>MigrationId exacto de <c>20260728234936_AddUserFuncionDescripcion.cs</c>.</summary>
    public const string AddUserFuncionDescripcionMigrationId = "20260728234936_AddUserFuncionDescripcion";

    /// <summary>ProductVersion de EF Core usada al generar las migraciones.</summary>
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

        // (c) BD legacy con tablas. Solo baseline de InitialCreate si el esquema base coincide;
        // si falta algo que InitialCreate asume, NO ocultar el desfase.
        if (!await LooksLikeInitialCreateSchemaAsync(context, cancellationToken))
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

        await StampMigrationAsync(context, InitialCreateMigrationId, cancellationToken);

        // Si la tabla de la migración posterior ya existe, marcarla también
        // (evita CREATE TABLE duplicado al correr MigrateAsync).
        if (await TableExistsAsync(context, "PasswordResetTokens", cancellationToken))
            await StampMigrationAsync(context, AddPasswordResetTokensMigrationId, cancellationToken);
    }

    private static async Task StampMigrationAsync(
        SipitexDbContext context,
        string migrationId,
        CancellationToken cancellationToken)
    {
        if (await MigrationRowExistsAsync(context, migrationId, cancellationToken))
            return;

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({0}, {1});
            """,
            migrationId,
            EfProductVersion);
    }

    private static async Task<bool> LooksLikeInitialCreateSchemaAsync(
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
