using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class CostoPromedioPonderadoMigrationTests
{
    private static SipitexDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new SipitexDbContext(options);
    }

    [Fact]
    public async Task MigrateFromInitialCreate_InicializaCostoPromedioPonderadoConCostoAdquisicion()
    {
        var databaseName = PostgresTestSupport.CreateDatabase();
        var connectionString = PostgresTestSupport.BuildConnectionString(databaseName);
        try
        {
            await using (var context = CreateContext(connectionString))
            {
                var initialId = MigrationBaseline.InitialCreateMigrationId(context);
                await context.Database.GetService<IMigrator>().MigrateAsync(initialId);
            }

            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    """
                    INSERT INTO "Materials"
                        ("Code", "Name", "Unit", "Stock", "MinStock", "Status", "LastEntryDate", "CostoAdquisicion", "PlantaInventarioId")
                    VALUES
                        ('mat-wac', 'Tela legado', 0, 8, 10, 0, DATE '2026-01-15', 7.2500, 1);
                    """;
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var context = CreateContext(connectionString))
            {
                await context.Database.MigrateAsync();
            }

            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    """
                    SELECT "CostoPromedioPonderado", "CostoAdquisicion"
                    FROM "Materials"
                    WHERE "Code" = 'mat-wac';
                    """;
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal(7.2500m, reader.GetDecimal(0));
                Assert.Equal(7.2500m, reader.GetDecimal(1));
            }

            await using (var context = CreateContext(connectionString))
            {
                Assert.True(await SchemaIntrospection.ColumnExistsAsync(context, "Materials", "CostoPromedioPonderado"));
                Assert.True(await SchemaIntrospection.ColumnExistsAsync(context, "ConsumosMaterial", "CostoUnitarioAlMomento"));
                Assert.False(await SchemaIntrospection.ColumnExistsAsync(context, "ConsumosMaterial", "CostoUnitario"));
            }
        }
        finally
        {
            PostgresTestSupport.DropDatabase(databaseName);
        }
    }
}
