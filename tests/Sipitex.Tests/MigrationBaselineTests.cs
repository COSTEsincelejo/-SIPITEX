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
            Assert.Equal(5, await CountMigrationRowsAsync(dbPath));
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
            Assert.Equal(5, await CountMigrationRowsAsync(dbPath));

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
            Assert.Equal(5, before);

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

    [Fact]
    public async Task LegacyPhotoPathColumnWithoutHistory_Initialize_DoesNotFail()
    {
        var dbPath = NewTempDbPath();
        try
        {
            // Esquema hasta AddFichaTurno, con PhotoPath ya presente (legacy) pero sin esas migraciones.
            await using (var context = CreateContext(dbPath))
            {
                await context.Database.MigrateAsync("20260728231835_AddFichaTurno");
            }

            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using (var alter = conn.CreateCommand())
                {
                    alter.CommandText = """ALTER TABLE "Users" ADD COLUMN "PhotoPath" TEXT NULL;""";
                    await alter.ExecuteNonQueryAsync();
                }

                await using (var del = conn.CreateCommand())
                {
                    del.CommandText =
                        """
                        DELETE FROM "__EFMigrationsHistory"
                        WHERE "MigrationId" IN ($p1, $p2);
                        """;
                    var p1 = del.CreateParameter();
                    p1.ParameterName = "$p1";
                    p1.Value = MigrationBaseline.AddUserPhotoPathMigrationId;
                    del.Parameters.Add(p1);
                    var p2 = del.CreateParameter();
                    p2.ParameterName = "$p2";
                    p2.Value = MigrationBaseline.AddUserFuncionDescripcionMigrationId;
                    del.Parameters.Add(p2);
                    await del.ExecuteNonQueryAsync();
                }
            }

            await using (var context = CreateContext(dbPath))
            {
                await DbInitializer.InitializeAsync(context);
            }

            Assert.Equal(5, await CountMigrationRowsAsync(dbPath));
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """PRAGMA table_info("Users");""";
                await using var reader = await cmd.ExecuteReaderAsync();
                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (await reader.ReadAsync())
                    columns.Add(reader.GetString(1));
                Assert.Contains("PhotoPath", columns);
                Assert.Contains("FuncionDescripcion", columns);
            }
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
