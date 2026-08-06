using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class MigrationBaselineTests
{
    private static SipitexDbContext CreateContext(string dbPath)
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        return new SipitexDbContext(options);
    }

    private static string NewTempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"sipitex-baseline-{Guid.NewGuid():N}.db");

    private static async Task<int> CountMigrationRowsAsync(string dbPath)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """SELECT COUNT(*) FROM "__EFMigrationsHistory";""";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<bool> TableExistsAsync(string dbPath, string table)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$n LIMIT 1;";
        var p = cmd.CreateParameter();
        p.ParameterName = "$n";
        p.Value = table;
        cmd.Parameters.Add(p);
        return await cmd.ExecuteScalarAsync() is not null and not DBNull;
    }

    [Fact]
    public async Task NewDatabase_MigrateAsync_CreatesFullSchema()
    {
        var dbPath = NewTempDbPath();
        try
        {
            await using (var context = CreateContext(dbPath))
            {
                await MigrationBaseline.EnsureBaselineAsync(context);
                await context.Database.MigrateAsync();
            }

            Assert.True(await TableExistsAsync(dbPath, "Materials"));
            Assert.True(await TableExistsAsync(dbPath, "ProductionSessions"));
            Assert.True(await TableExistsAsync(dbPath, "Users"));
            Assert.True(await TableExistsAsync(dbPath, "__EFMigrationsHistory"));
            // ... + AddOrderMaterialRequirements = 11
            Assert.Equal(11, await CountMigrationRowsAsync(dbPath));
            Assert.True(await TableExistsAsync(dbPath, "FichaInstructors"));
            Assert.True(await TableExistsAsync(dbPath, "SolicitudesMaterial"));
            Assert.True(await TableExistsAsync(dbPath, "DetallesSolicitudMaterial"));
            Assert.True(await TableExistsAsync(dbPath, "EntregasMaterial"));
            Assert.True(await TableExistsAsync(dbPath, "BomProducts"));
            Assert.True(await TableExistsAsync(dbPath, "ProductionOrderMaterialRequirements"));
            Assert.True(await TableExistsAsync(dbPath, "ProductionOrderBomSnapshots"));
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task LegacyFullSchemaWithoutHistory_BaselineThenMigrate_Succeeds()
    {
        var dbPath = NewTempDbPath();
        try
        {
            // Esquema legacy = hasta AddPasswordResetTokens (sin AddFichaTurno / PhotoPath).
            // Así el baseline marca las dos primeras y MigrateAsync aplica el resto.
            await using (var context = CreateContext(dbPath))
            {
                await context.Database.MigrateAsync(MigrationBaseline.AddPasswordResetTokensMigrationId);
            }

            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """DROP TABLE IF EXISTS "__EFMigrationsHistory";""";
                await cmd.ExecuteNonQueryAsync();
            }

            Assert.False(await TableExistsAsync(dbPath, "__EFMigrationsHistory"));
            Assert.True(await TableExistsAsync(dbPath, "Materials"));

            await using (var context = CreateContext(dbPath))
            {
                await MigrationBaseline.EnsureBaselineAsync(context);
                await context.Database.MigrateAsync(); // aplica migraciones posteriores al baseline
            }

            Assert.True(await TableExistsAsync(dbPath, "__EFMigrationsHistory"));
            Assert.Equal(11, await CountMigrationRowsAsync(dbPath));
            Assert.True(await TableExistsAsync(dbPath, "FichaInstructors"));
            Assert.True(await TableExistsAsync(dbPath, "SolicitudesMaterial"));
            Assert.True(await TableExistsAsync(dbPath, "BomProducts"));
            Assert.True(await TableExistsAsync(dbPath, "ProductionOrderMaterialRequirements"));

            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    """SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";""";
                await using var reader = await cmd.ExecuteReaderAsync();
                var ids = new List<string>();
                while (await reader.ReadAsync())
                    ids.Add(reader.GetString(0));
                Assert.Contains(MigrationBaseline.InitialCreateMigrationId, ids);
                Assert.Contains(MigrationBaseline.AddPasswordResetTokensMigrationId, ids);
                Assert.Contains(ids, id => id.Contains("AddFichaTurno", StringComparison.Ordinal));
                Assert.Contains(ids, id => id.Contains("AddUserPhotoPath", StringComparison.Ordinal));
                Assert.Contains(ids, id => id.Contains("AddUserFuncionDescripcion", StringComparison.Ordinal));
                Assert.Contains(ids, id => id.Contains("AddFichaInstructors", StringComparison.Ordinal));
                Assert.Contains(ids, id => id.Contains("AddFichaAssignedOrderText", StringComparison.Ordinal));
                Assert.Contains(ids, id => id.Contains("AddFichaInstructorProceso", StringComparison.Ordinal));
                Assert.Contains(ids, id => id.Contains("AddSolicitudMaterial", StringComparison.Ordinal));
                Assert.Contains(ids, id => id.Contains("AddBomProductAndOrderSnapshot", StringComparison.Ordinal));
                Assert.Contains(ids, id => id.Contains("AddOrderMaterialRequirements", StringComparison.Ordinal));
            }
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task ExistingMigrationsHistory_EnsureBaseline_DoesNothing()
    {
        var dbPath = NewTempDbPath();
        try
        {
            await using (var context = CreateContext(dbPath))
            {
                await context.Database.MigrateAsync();
            }

            var before = await CountMigrationRowsAsync(dbPath);
            Assert.Equal(11, before);

            await using (var context = CreateContext(dbPath))
            {
                await MigrationBaseline.EnsureBaselineAsync(context);
                await MigrationBaseline.EnsureBaselineAsync(context); // segunda vez idempotente
            }

            var after = await CountMigrationRowsAsync(dbPath);
            Assert.Equal(before, after);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
