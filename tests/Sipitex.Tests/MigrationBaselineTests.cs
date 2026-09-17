using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class MigrationBaselineTests
{
    private static SipitexDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new SipitexDbContext(options);
    }

    private static int CountRegisteredMigrations()
    {
        using var context = CreateContext("Host=localhost;Database=unused;Username=u;Password=p");
        return context.Database.GetMigrations().Count();
    }

    private static async Task<int> CountMigrationRowsAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """SELECT COUNT(*) FROM "__EFMigrationsHistory";""";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<bool> TableExistsAsync(string connectionString, string table)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = ANY (current_schemas(false))
              AND table_name = @n
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("n", table);
        return await cmd.ExecuteScalarAsync() is not null and not DBNull;
    }

    [Fact]
    public async Task NewDatabase_MigrateAsync_CreatesFullSchema()
    {
        var databaseName = PostgresTestSupport.CreateDatabase();
        var connectionString = PostgresTestSupport.BuildConnectionString(databaseName);
        try
        {
            await using (var context = CreateContext(connectionString))
            {
                await MigrationBaseline.EnsureBaselineAsync(context);
                await context.Database.MigrateAsync();
            }

            Assert.True(await TableExistsAsync(connectionString, "Materials"));
            Assert.True(await TableExistsAsync(connectionString, "ProductionSessions"));
            Assert.True(await TableExistsAsync(connectionString, "Users"));
            Assert.True(await TableExistsAsync(connectionString, "__EFMigrationsHistory"));
            Assert.Equal(CountRegisteredMigrations(), await CountMigrationRowsAsync(connectionString));
            Assert.True(await TableExistsAsync(connectionString, "PlantasInventario"));
            Assert.True(await TableExistsAsync(connectionString, "UserPlantasInventario"));
            Assert.True(await TableExistsAsync(connectionString, "FichaInstructors"));
            Assert.True(await TableExistsAsync(connectionString, "SolicitudesMaterial"));
            Assert.True(await TableExistsAsync(connectionString, "DetallesSolicitudMaterial"));
            Assert.True(await TableExistsAsync(connectionString, "EntregasMaterial"));
            Assert.True(await TableExistsAsync(connectionString, "BomProducts"));
            Assert.True(await TableExistsAsync(connectionString, "BomProductInstructors"));
            Assert.True(await TableExistsAsync(connectionString, "BomProductTallas"));
            Assert.True(await TableExistsAsync(connectionString, "BomProductPiezas"));
            Assert.True(await TableExistsAsync(connectionString, "BomProductMedidas"));
            Assert.True(await TableExistsAsync(connectionString, "BomProductMedidaValores"));
            Assert.True(await TableExistsAsync(connectionString, "ProductionOrderMaterialRequirements"));
            Assert.True(await TableExistsAsync(connectionString, "ProductionOrderStages"));
            Assert.True(await TableExistsAsync(connectionString, "ProductionOrderBomSnapshots"));
            Assert.True(await TableExistsAsync(connectionString, "StockMovements"));
            Assert.True(await TableExistsAsync(connectionString, "OrderChangeLogs"));
            Assert.True(await TableExistsAsync(connectionString, "ActivityLogs"));
            Assert.True(await TableExistsAsync(connectionString, "ConsumosMaterial"));
            Assert.True(await TableExistsAsync(connectionString, "GruposConfeccion"));
            Assert.True(await TableExistsAsync(connectionString, "ActasMovimiento"));
            Assert.True(await TableExistsAsync(connectionString, "ActasMovimientoDetalle"));
            Assert.True(await TableExistsAsync(connectionString, "AppSettings"));
            Assert.True(await TableExistsAsync(connectionString, "PrendasTrazables"));
            Assert.True(await TableExistsAsync(connectionString, "MaterialRequests"));
        }
        finally
        {
            PostgresTestSupport.DropDatabase(databaseName);
        }
    }

    [Fact]
    public async Task LegacyFullSchemaWithoutHistory_BaselineThenMigrate_Succeeds()
    {
        var databaseName = PostgresTestSupport.CreateDatabase();
        var connectionString = PostgresTestSupport.BuildConnectionString(databaseName);
        try
        {
            string initialId;
            await using (var context = CreateContext(connectionString))
            {
                initialId = MigrationBaseline.InitialCreateMigrationId(context);
                await context.Database.GetService<IMigrator>().MigrateAsync(initialId);
            }

            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """DROP TABLE IF EXISTS "__EFMigrationsHistory";""";
                await cmd.ExecuteNonQueryAsync();
            }

            Assert.False(await TableExistsAsync(connectionString, "__EFMigrationsHistory"));
            Assert.True(await TableExistsAsync(connectionString, "Materials"));

            await using (var context = CreateContext(connectionString))
            {
                await MigrationBaseline.EnsureBaselineAsync(context);
                await context.Database.MigrateAsync();
            }

            Assert.True(await TableExistsAsync(connectionString, "__EFMigrationsHistory"));
            Assert.Equal(CountRegisteredMigrations(), await CountMigrationRowsAsync(connectionString));
            Assert.True(await TableExistsAsync(connectionString, "PrendasTrazables"));

            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    """
                    SELECT "MigrationId"
                    FROM "__EFMigrationsHistory"
                    ORDER BY "MigrationId"
                    LIMIT 1;
                    """;
                var id = (string?)await cmd.ExecuteScalarAsync();
                Assert.Equal(initialId, id);
                Assert.Contains("InitialCreate", id, StringComparison.Ordinal);
            }
        }
        finally
        {
            PostgresTestSupport.DropDatabase(databaseName);
        }
    }

    [Fact]
    public async Task ExistingMigrationsHistory_EnsureBaseline_DoesNothing()
    {
        var databaseName = PostgresTestSupport.CreateDatabase();
        var connectionString = PostgresTestSupport.BuildConnectionString(databaseName);
        try
        {
            await using (var context = CreateContext(connectionString))
            {
                await context.Database.MigrateAsync();
            }

            var before = await CountMigrationRowsAsync(connectionString);
            Assert.Equal(CountRegisteredMigrations(), before);

            await using (var context = CreateContext(connectionString))
            {
                await MigrationBaseline.EnsureBaselineAsync(context);
                await MigrationBaseline.EnsureBaselineAsync(context);
            }

            Assert.Equal(before, await CountMigrationRowsAsync(connectionString));
        }
        finally
        {
            PostgresTestSupport.DropDatabase(databaseName);
        }
    }
}
