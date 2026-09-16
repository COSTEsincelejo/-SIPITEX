using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

/// <summary>
/// Tipo de solicitud: default PorFicha en el modelo y en InitialCreate de PostgreSQL.
/// </summary>
public class SolicitudMaterialTipoMigrationTests
{
    [Fact]
    public void InitialCreate_TipoColumn_DefaultValueIsPorFicha()
    {
        var migrationsDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Sipitex.Infrastructure", "Migrations"));
        var path = Directory.GetFiles(migrationsDir, "*_InitialCreate.cs")
            .FirstOrDefault(p => !p.EndsWith(".Designer.cs", StringComparison.Ordinal));
        Assert.True(path is not null, $"No se encontró InitialCreate en {migrationsDir}");

        var source = File.ReadAllText(path);
        Assert.Contains("name: \"Tipo\"", source, StringComparison.Ordinal);
        Assert.Contains("defaultValue: \"PorFicha\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("defaultValue: \"\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NewSolicitud_TipoDefaultsToPorFicha_ViaEfConversion()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"sipitex-tipo-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<SipitexDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            await using var context = new SipitexDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var user = new User
            {
                Nombre = "User Tipo",
                Email = "tipo@test.local",
                PasswordHash = "x",
                Rol = UserRoles.Instructor,
                IsActive = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var ficha = new Ficha
            {
                NumeroGrupo = "FICHA-TIPO",
                ProcessName = "Corte",
                InstructorName = user.Nombre,
                Turno = "Mañana"
            };
            context.Fichas.Add(ficha);
            await context.SaveChangesAsync();

            var solicitud = new SolicitudMaterial
            {
                Codigo = "SOL-TIPO-1",
                FichaId = ficha.Id,
                SolicitanteId = user.Id
            };
            context.SolicitudesMaterial.Add(solicitud);
            await context.SaveChangesAsync();

            var loaded = await context.SolicitudesMaterial.AsNoTracking()
                .SingleAsync(s => s.Codigo == "SOL-TIPO-1");
            Assert.Equal(SolicitudMaterialTipo.PorFicha, loaded.Tipo);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
