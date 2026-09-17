using Microsoft.EntityFrameworkCore;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Data;

// Para BDs que se crearon antes con EnsureCreated o SQL a mano:
// si ya tienen tablas pero no __EFMigrationsHistory, les "estampo" las migraciones
// que ya están reflejadas en el esquema. Solo CREATE/INSERT, nunca DROP.
public static class MigrationBaseline
{
    public const string EfProductVersion = "10.0.9";

    public static string InitialCreateMigrationId(SipitexDbContext context)
    {
        var first = context.Database.GetMigrations().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(first))
            throw new InvalidOperationException("No hay migraciones EF Core registradas.");
        return first;
    }

    public static async Task EnsureBaselineAsync(
        SipitexDbContext context,
        CancellationToken cancellationToken = default)
    {
        if (await SchemaIntrospection.TableExistsAsync(context, "__EFMigrationsHistory", cancellationToken))
            return;

        if (!await SchemaIntrospection.TableExistsAsync(context, "Materials", cancellationToken))
            return;

        if (!await LooksLikeInitialCreateSchemaAsync(context, cancellationToken))
        {
            throw new InvalidOperationException(
                "La base de datos tiene tablas de negocio pero el esquema no coincide con InitialCreate " +
                "(faltan columnas o tablas esperadas: p. ej. Materials.LastEntryDate, " +
                "QualityRecords.MotivoReproceso/Responsable, ProductionSessions, Users). " +
                "No se aplicó baseline automático. Haga backup y revise el desfase antes de continuar.");
        }

        if (SchemaIntrospection.IsNpgsql(context))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """,
                cancellationToken);
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """,
                cancellationToken);
        }

        await StampMigrationAsync(context, InitialCreateMigrationId(context), cancellationToken);
    }

    private static async Task StampMigrationAsync(
        SipitexDbContext context,
        string migrationId,
        CancellationToken cancellationToken)
    {
        if (await SchemaIntrospection.MigrationRowExistsAsync(context, migrationId, cancellationToken))
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
        if (!await SchemaIntrospection.ColumnExistsAsync(context, "Materials", "LastEntryDate", cancellationToken))
            return false;
        if (!await SchemaIntrospection.TableExistsAsync(context, "QualityRecords", cancellationToken))
            return false;
        if (!await SchemaIntrospection.ColumnExistsAsync(context, "QualityRecords", "MotivoReproceso", cancellationToken))
            return false;
        if (!await SchemaIntrospection.ColumnExistsAsync(context, "QualityRecords", "Responsable", cancellationToken))
            return false;
        if (!await SchemaIntrospection.TableExistsAsync(context, "ProductionSessions", cancellationToken))
            return false;
        if (!await SchemaIntrospection.TableExistsAsync(context, "Users", cancellationToken))
            return false;
        return true;
    }
}
