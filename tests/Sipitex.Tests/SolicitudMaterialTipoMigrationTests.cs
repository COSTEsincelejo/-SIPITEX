using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

/// <summary>
/// Evidencia: backfill de Tipo="PorFicha" en filas pre-migración + HasConversion&lt;string&gt;.
/// </summary>
public class SolicitudMaterialTipoMigrationTests
{
    private const string PreviousMigrationId = "20260812185854_AddActivityLog";

    private static string NewTempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"sipitex-tipo-mig-{Guid.NewGuid():N}.db");

    private static SipitexDbContext CreateContext(string dbPath)
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        return new SipitexDbContext(options);
    }

    [Fact]
    public void MigrationUp_TipoColumn_DefaultValueIsPorFicha_NotEmptyString()
    {
        // bin/Debug/netX.0 → 5 niveles hasta la raíz del repo
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Sipitex.Infrastructure", "Migrations",
            "20260813030312_AddSolicitudMaterialInsumosLibres.cs"));
        Assert.True(File.Exists(path), $"No se encontró la migración en {path}");

        var source = File.ReadAllText(path);
        Assert.Contains("name: \"Tipo\"", source, StringComparison.Ordinal);
        Assert.Contains("defaultValue: \"PorFicha\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("defaultValue: \"\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreMigrationRow_AfterMigrate_TipoIsPorFicha_ViaSqlDefaultAndEfConversion()
    {
        var dbPath = NewTempDbPath();
        try
        {
            // 1) Esquema hasta la migración anterior (sin columna Tipo)
            await using (var context = CreateContext(dbPath))
            {
                await context.Database.MigrateAsync(PreviousMigrationId);
            }

            // 2) Insertar fila como existía antes (SQL crudo — sin columna Tipo)
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();

                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        """
                        INSERT INTO "Users" ("Nombre", "Email", "PasswordHash", "Rol", "PermisosExtendidos", "IsActive")
                        VALUES ('User Mig', 'mig@test.local', 'x', 'Instructor', '', 1);
                        """;
                    await cmd.ExecuteNonQueryAsync();
                }

                long userId;
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = """SELECT last_insert_rowid();""";
                    userId = (long)(await cmd.ExecuteScalarAsync())!;
                }

                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        """
                        INSERT INTO "Fichas" ("FichaCode", "ProcessName", "InstructorName", "Turno")
                        VALUES ('FICHA-MIG', 'Corte', 'User Mig', '');
                        """;
                    await cmd.ExecuteNonQueryAsync();
                }

                long fichaId;
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = """SELECT last_insert_rowid();""";
                    fichaId = (long)(await cmd.ExecuteScalarAsync())!;
                }

                // Confirmar que Tipo aún no existe
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = """PRAGMA table_info("SolicitudesMaterial");""";
                    await using var reader = await cmd.ExecuteReaderAsync();
                    var cols = new List<string>();
                    while (await reader.ReadAsync())
                        cols.Add(reader.GetString(1));
                    Assert.DoesNotContain("Tipo", cols);
                }

                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        """
                        INSERT INTO "SolicitudesMaterial"
                            ("Codigo", "FichaId", "SolicitanteId", "Estado", "FechaSolicitud", "Observaciones")
                        VALUES
                            ('SOL-MIG-1', $ficha, $user, 'Pendiente', $fecha, NULL);
                        """;
                    var pFicha = cmd.CreateParameter();
                    pFicha.ParameterName = "$ficha";
                    pFicha.Value = fichaId;
                    cmd.Parameters.Add(pFicha);
                    var pUser = cmd.CreateParameter();
                    pUser.ParameterName = "$user";
                    pUser.Value = userId;
                    cmd.Parameters.Add(pUser);
                    var pFecha = cmd.CreateParameter();
                    pFecha.ParameterName = "$fecha";
                    pFecha.Value = DateTime.UtcNow.ToString("O");
                    cmd.Parameters.Add(pFecha);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            // 3) Aplicar migraciones restantes (incluye AddSolicitudMaterialInsumosLibres)
            await using (var context = CreateContext(dbPath))
            {
                await context.Database.MigrateAsync();
            }

            // 4) Evidencia SQL: default backfill = 'PorFicha' (no vacío)
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    """SELECT "Tipo" FROM "SolicitudesMaterial" WHERE "Codigo" = 'SOL-MIG-1';""";
                var tipoSql = (string?)await cmd.ExecuteScalarAsync();
                Assert.Equal("PorFicha", tipoSql);
            }

            // 5) Evidencia EF: HasConversion<string> deserializa a enum PorFicha
            await using (var context = CreateContext(dbPath))
            {
                var solicitud = await context.SolicitudesMaterial
                    .AsNoTracking()
                    .SingleAsync(s => s.Codigo == "SOL-MIG-1");
                Assert.Equal(SolicitudMaterialTipo.PorFicha, solicitud.Tipo);
                Assert.Equal("FICHA-MIG", (await context.Fichas.AsNoTracking()
                    .SingleAsync(f => f.Id == solicitud.FichaId)).FichaCode);
            }
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var p = dbPath + suffix;
                if (File.Exists(p)) File.Delete(p);
            }
        }
    }

    [Fact]
    public async Task HasConversion_RoundTrip_PersistsPorFichaAsString()
    {
        var dbPath = NewTempDbPath();
        try
        {
            await using (var context = CreateContext(dbPath))
            {
                await context.Database.MigrateAsync();

                var user = new User
                {
                    Nombre = "Round Trip",
                    Email = $"rt-{Guid.NewGuid():N}@test.local",
                    PasswordHash = "x",
                    Rol = UserRoles.Instructor,
                    IsActive = true
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                var ficha = new Ficha
                {
                    FichaCode = "F-RT",
                    ProcessName = "Corte",
                    InstructorName = "Round Trip",
                    Turno = ""
                };
                context.Fichas.Add(ficha);
                await context.SaveChangesAsync();

                context.SolicitudesMaterial.Add(new SolicitudMaterial
                {
                    Codigo = "SOL-RT-1",
                    Tipo = SolicitudMaterialTipo.PorFicha,
                    FichaId = ficha.Id,
                    SolicitanteId = user.Id,
                    Estado = SolicitudMaterialEstado.Pendiente,
                    FechaSolicitud = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """SELECT "Tipo" FROM "SolicitudesMaterial" WHERE "Codigo" = 'SOL-RT-1';""";
                Assert.Equal("PorFicha", (string?)await cmd.ExecuteScalarAsync());
            }

            await using (var context = CreateContext(dbPath))
            {
                var loaded = await context.SolicitudesMaterial.AsNoTracking()
                    .SingleAsync(s => s.Codigo == "SOL-RT-1");
                Assert.Equal(SolicitudMaterialTipo.PorFicha, loaded.Tipo);
            }
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var p = dbPath + suffix;
                if (File.Exists(p)) File.Delete(p);
            }
        }
    }
}
