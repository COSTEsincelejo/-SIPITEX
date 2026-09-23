using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Sipitex.Infrastructure.Data;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

public class EmailConfirmedMigrationTests
{
    private const string PreviousMigration = "20260917113326_AddCostoPromedioPonderadoYSnapshotConsumo";

    private static SipitexDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new SipitexDbContext(options);
    }

    [Fact]
    public async Task UsuariosExistentes_QuedanConCorreoConfirmado()
    {
        var databaseName = PostgresTestSupport.CreateDatabase();
        var connectionString = PostgresTestSupport.BuildConnectionString(databaseName);
        try
        {
            await using (var context = CreateContext(connectionString))
            {
                await context.Database.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            }

            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    """
                    INSERT INTO "Users"
                        ("Nombre", "Email", "PasswordHash", "Rol", "PermisosExtendidos", "IsActive")
                    VALUES
                        ('Cuenta previa', 'previa@sipitex.test', 'hash', 'Instructor', '', TRUE);
                    """;
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var context = CreateContext(connectionString))
            {
                await context.Database.MigrateAsync();
                Assert.True(await SchemaIntrospection.ColumnExistsAsync(context, "Users", "EmailConfirmed"));
                Assert.True(await SchemaIntrospection.ColumnExistsAsync(context, "PasswordResetTokens", "Purpose"));
                Assert.True(await SchemaIntrospection.ColumnExistsAsync(context, "PasswordResetTokens", "FailedAttempts"));
            }

            await using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    """
                    SELECT "EmailConfirmed"
                    FROM "Users"
                    WHERE "Email" = 'previa@sipitex.test';
                    """;
                var value = await cmd.ExecuteScalarAsync();
                Assert.Equal(true, value);
            }
        }
        finally
        {
            PostgresTestSupport.DropDatabase(databaseName);
        }
    }
}
