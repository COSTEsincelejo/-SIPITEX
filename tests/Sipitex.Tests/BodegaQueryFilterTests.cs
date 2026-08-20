using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

/// <summary>
/// Global Query Filters de Material/SolicitudMaterial según ICurrentBodegaAccessor.
/// Consulta directa al DbContext, sin servicios.
/// </summary>
public class BodegaQueryFilterTests
{
    [Fact]
    public async Task Materials_BodegueroBodega1_SoloVeFilasDeBodega1()
    {
        var path = TempDb();
        await SeedMaterialsAsync(path);

        await using var db = Create(path, new FixedCurrentBodegaAccessor([1]));
        var list = await db.Materials.AsNoTracking().ToListAsync();

        Assert.Equal(2, list.Count);
        Assert.All(list, m => Assert.Equal(1, m.BodegaId));
        Assert.DoesNotContain(list, m => m.Code == "mat-b2");
    }

    [Fact]
    public async Task Materials_BodegueroConDosBodegas_VeAmbasYNingunaOtra()
    {
        var path = TempDb();
        await SeedMaterialsAsync(path, includeBodega3: true);

        await using var db = Create(path, new FixedCurrentBodegaAccessor([1, 2]));
        var list = await db.Materials.AsNoTracking().ToListAsync();

        Assert.Equal(3, list.Count);
        Assert.Contains(list, m => m.Code == "mat-b1a");
        Assert.Contains(list, m => m.Code == "mat-b2");
        Assert.DoesNotContain(list, m => m.Code == "mat-b3");
    }

    [Fact]
    public async Task Materials_QueryFilterContains_SeTraduceASql()
    {
        var path = TempDb();
        await SeedMaterialsAsync(path);

        await using var db = Create(path, new FixedCurrentBodegaAccessor([1, 2]));
        var sql = db.Materials.ToQueryString();

        Assert.Contains("BodegaId", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ClientEval", sql, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            sql.Contains("IN", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("json_each", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("OPENJSON", StringComparison.OrdinalIgnoreCase),
            $"Se esperaba traducción SQL IN/json, no evaluación en memoria. SQL: {sql}");
    }

    [Fact]
    public async Task Materials_AccessorNull_VeTodasLasBodegas()
    {
        var path = TempDb();
        await SeedMaterialsAsync(path);

        await using var db = Create(path, NullCurrentBodegaAccessor.Instance);
        var list = await db.Materials.AsNoTracking().ToListAsync();

        Assert.Equal(3, list.Count);
        Assert.Contains(list, m => m.Code == "mat-b2");
    }

    [Fact]
    public async Task Solicitudes_BodegueroBodega1_SoloVeFilasDeBodega1()
    {
        var path = TempDb();
        await SeedSolicitudesAsync(path);

        await using var db = Create(path, new FixedCurrentBodegaAccessor([1]));
        var list = await db.SolicitudesMaterial.AsNoTracking().ToListAsync();

        Assert.Single(list);
        Assert.Equal("SOL-B1", list[0].Codigo);
    }

    [Fact]
    public async Task Solicitudes_BodegueroConDosBodegas_VeAmbasYNingunaOtra()
    {
        var path = TempDb();
        await SeedSolicitudesAsync(path, includeBodega3: true);

        await using var db = Create(path, new FixedCurrentBodegaAccessor([1, 2]));
        var list = await db.SolicitudesMaterial.AsNoTracking().ToListAsync();

        Assert.Equal(2, list.Count);
        Assert.Contains(list, s => s.Codigo == "SOL-B1");
        Assert.Contains(list, s => s.Codigo == "SOL-B2");
        Assert.DoesNotContain(list, s => s.Codigo == "SOL-B3");
    }

    [Fact]
    public async Task Solicitudes_AccessorNull_VeTodasLasBodegas()
    {
        var path = TempDb();
        await SeedSolicitudesAsync(path);

        await using var db = Create(path, NullCurrentBodegaAccessor.Instance);
        var list = await db.SolicitudesMaterial.AsNoTracking().ToListAsync();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task Materials_BodegueroSinBodega_ListaVacia()
    {
        var path = TempDb();
        await SeedMaterialsAsync(path);

        await using var db = Create(path, new FixedCurrentBodegaAccessor([]));
        var list = await db.Materials.AsNoTracking().ToListAsync();

        Assert.Empty(list);
    }

    private static string TempDb() =>
        Path.Combine(Path.GetTempPath(), $"sipitex-gqf-{Guid.NewGuid():N}.db");

    private static SipitexDbContext Create(string path, ICurrentBodegaAccessor accessor) =>
        new(new DbContextOptionsBuilder<SipitexDbContext>().UseSqlite($"Data Source={path}").Options, accessor);

    private static async Task SeedMaterialsAsync(string path, bool includeBodega3 = false)
    {
        await using var db = Create(path, NullCurrentBodegaAccessor.Instance);
        await db.Database.EnsureCreatedAsync();
        if (includeBodega3)
        {
            db.Bodegas.Add(new Bodega { Nombre = "Bodega 3" });
            await db.SaveChangesAsync();
        }
        db.Materials.AddRange(
            new Material { Code = "mat-b1a", Name = "Tela 1", Unit = MaterialUnit.Metros, Stock = 10, BodegaId = 1 },
            new Material { Code = "mat-b1b", Name = "Hilo 1", Unit = MaterialUnit.Unidades, Stock = 5, BodegaId = 1 },
            new Material { Code = "mat-b2", Name = "Forro 2", Unit = MaterialUnit.Metros, Stock = 8, BodegaId = 2 });
        if (includeBodega3)
            db.Materials.Add(new Material { Code = "mat-b3", Name = "Botón 3", Unit = MaterialUnit.Unidades, Stock = 4, BodegaId = 3 });
        await db.SaveChangesAsync();
    }

    private static async Task SeedSolicitudesAsync(string path, bool includeBodega3 = false)
    {
        await using var db = Create(path, NullCurrentBodegaAccessor.Instance);
        await db.Database.EnsureCreatedAsync();
        if (includeBodega3)
        {
            db.Bodegas.Add(new Bodega { Nombre = "Bodega 3" });
            await db.SaveChangesAsync();
        }
        var instructor = new User
        {
            Nombre = "Laura",
            Email = $"laura-{Guid.NewGuid():N}@test.com",
            PasswordHash = "x",
            Rol = UserRoles.Instructor,
            IsActive = true
        };
        db.Users.Add(instructor);
        await db.SaveChangesAsync();

        db.SolicitudesMaterial.AddRange(
            new SolicitudMaterial
            {
                Codigo = "SOL-B1",
                SolicitanteId = instructor.Id,
                Estado = SolicitudMaterialEstado.Pendiente,
                BodegaId = 1
            },
            new SolicitudMaterial
            {
                Codigo = "SOL-B2",
                SolicitanteId = instructor.Id,
                Estado = SolicitudMaterialEstado.Pendiente,
                BodegaId = 2
            });
        if (includeBodega3)
        {
            db.SolicitudesMaterial.Add(new SolicitudMaterial
            {
                Codigo = "SOL-B3",
                SolicitanteId = instructor.Id,
                Estado = SolicitudMaterialEstado.Pendiente,
                BodegaId = 3
            });
        }

        await db.SaveChangesAsync();
    }
}
