using Microsoft.EntityFrameworkCore; // ExecuteSqlRawAsync y conexión ADO
using Sipitex.Infrastructure.Persistence; // SipitexDbContext

namespace Sipitex.Infrastructure.Data;

// Para BDs que se crearon antes con EnsureCreated o SQL a mano:
// si ya tienen tablas pero no __EFMigrationsHistory, les "estampo" las migraciones
// que ya están reflejadas en el esquema. Solo CREATE/INSERT, nunca DROP.
public static class MigrationBaseline
{
    // Tiene que coincidir exacto con el nombre del archivo de migración
    public const string InitialCreateMigrationId = "20260728214130_InitialCreate";

    // Segunda migración que a veces ya estaba aplicada a mano
    public const string AddPasswordResetTokensMigrationId = "20260728221016_AddPasswordResetTokens";

    // La versión de EF con la que generamos las migraciones
    public const string EfProductVersion = "10.0.9";

    // Punto de entrada: lo llama DbInitializer antes de MigrateAsync
    public static async Task EnsureBaselineAsync(
        SipitexDbContext context,
        CancellationToken cancellationToken = default)
    {
        // Si ya hay historial de migraciones, no hago nada
        if (await TableExistsAsync(context, "__EFMigrationsHistory", cancellationToken))
            return;

        // BD nueva sin tablas → MigrateAsync se encarga de todo
        if (!await TableExistsAsync(context, "Materials", cancellationToken))
            return;

        // Hay tablas pero sin historial = BD legacy. Verifico que el esquema calce con InitialCreate
        if (!await LooksLikeInitialCreateSchemaAsync(context, cancellationToken))
        {
            // Si el esquema no cuadra, mejor tirar error que romper la BD
            throw new InvalidOperationException(
                "sipitex.db tiene tablas de negocio pero el esquema no coincide con InitialCreate " +
                "(faltan columnas o tablas esperadas: p. ej. Materials.LastEntryDate, " +
                "QualityRecords.MotivoReproceso/Responsable, ProductionSessions, Users). " +
                "No se aplicó baseline automático. Haga backup (cp sipitex.db sipitex.db.bak) " +
                "y revise el desfase antes de continuar.");
        }

        // Creo la tabla de historial si no existía (caso legacy)
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """,
            cancellationToken);

        // Marco InitialCreate como ya aplicada
        await StampMigrationAsync(context, InitialCreateMigrationId, cancellationToken);

        // Si PasswordResetTokens ya existe, esa migración también ya corrió de facto
        if (await TableExistsAsync(context, "PasswordResetTokens", cancellationToken))
            await StampMigrationAsync(context, AddPasswordResetTokensMigrationId, cancellationToken);
    }

    // Inserta la fila en __EFMigrationsHistory para que EF no vuelva a aplicar esa migración
    private static async Task StampMigrationAsync(
        SipitexDbContext context,
        string migrationId,
        CancellationToken cancellationToken)
    {
        if (await MigrationRowExistsAsync(context, migrationId, cancellationToken))
            return; // Ya estaba estampada

        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({0}, {1});
            """,
            migrationId,
            EfProductVersion);
    }

    // Reviso columnas/tablas clave que InitialCreate debería haber creado
    private static async Task<bool> LooksLikeInitialCreateSchemaAsync(
        SipitexDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(context, "Materials", "LastEntryDate", cancellationToken))
            return false; // Falta columna de fecha de entrada
        if (!await TableExistsAsync(context, "QualityRecords", cancellationToken))
            return false; // No hay tabla de calidad
        if (!await ColumnExistsAsync(context, "QualityRecords", "MotivoReproceso", cancellationToken))
            return false; // Falta motivo de reproceso
        if (!await ColumnExistsAsync(context, "QualityRecords", "Responsable", cancellationToken))
            return false; // Falta responsable
        if (!await TableExistsAsync(context, "ProductionSessions", cancellationToken))
            return false; // No hay sesiones de producción
        if (!await TableExistsAsync(context, "Users", cancellationToken))
            return false; // No hay usuarios
        return true; // El esquema parece el de InitialCreate
    }

    // Consulta sqlite_master — también lo usa DbInitializer
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

    // PRAGMA table_info para ver si una columna existe
    private static async Task<bool> ColumnExistsAsync(
        SipitexDbContext context,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // Por si acaso, no meto nombres raros en el PRAGMA
        if (!IsSafeSqliteIdentifier(tableName) || !IsSafeSqliteIdentifier(columnName))
            return false; // Nombre sospechoso, no ejecuto
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // Evita insertar dos veces la misma migración en el historial
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

    // Solo letras, números y guión bajo — nada de comillas raras en el PRAGMA
    private static bool IsSafeSqliteIdentifier(string name) =>
        name.Length > 0 && name.All(c => char.IsLetterOrDigit(c) || c == '_');
}
