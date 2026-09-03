using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Tests;

/// <summary>
/// Global Query Filters de Material/SolicitudMaterial según ICurrentPlantaInventarioAccessor.
/// Consulta directa al DbContext, sin servicios.
/// </summary>
public class PlantaInventarioQueryFilterTests
{
    [Fact]
    public async Task Materials_BodegueroBodega1_SoloVeFilasDeBodega1()
    {
        var path = TempDb();
        await SeedMaterialsAsync(path);

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([1]));
        var list = await db.Materials.AsNoTracking().ToListAsync();

        Assert.Equal(2, list.Count);
        Assert.All(list, m => Assert.Equal(1, m.PlantaInventarioId));
        Assert.DoesNotContain(list, m => m.Code == "mat-b2");
    }

    [Fact]
    public async Task Materials_BodegueroConDosBodegas_VeAmbasYNingunaOtra()
    {
        var path = TempDb();
        await SeedMaterialsAsync(path, includeBodega3: true);

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([1, 2]));
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

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([1, 2]));
        var sql = db.Materials.ToQueryString();

        Assert.Contains("PlantaInventarioId", sql, StringComparison.OrdinalIgnoreCase);
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

        await using var db = Create(path, NullCurrentPlantaInventarioAccessor.Instance);
        var list = await db.Materials.AsNoTracking().ToListAsync();

        Assert.Equal(3, list.Count);
        Assert.Contains(list, m => m.Code == "mat-b2");
    }

    [Fact]
    public async Task Solicitudes_BodegueroBodega1_SoloVeFilasDeBodega1()
    {
        var path = TempDb();
        await SeedSolicitudesAsync(path);

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([1]));
        var list = await db.SolicitudesMaterial.AsNoTracking().ToListAsync();

        Assert.Single(list);
        Assert.Equal("SOL-B1", list[0].Codigo);
    }

    [Fact]
    public async Task Solicitudes_BodegueroConDosBodegas_VeAmbasYNingunaOtra()
    {
        var path = TempDb();
        await SeedSolicitudesAsync(path, includeBodega3: true);

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([1, 2]));
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

        await using var db = Create(path, NullCurrentPlantaInventarioAccessor.Instance);
        var list = await db.SolicitudesMaterial.AsNoTracking().ToListAsync();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task Materials_BodegueroSinBodega_ListaVacia()
    {
        var path = TempDb();
        await SeedMaterialsAsync(path);

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([]));
        var list = await db.Materials.AsNoTracking().ToListAsync();

        Assert.Empty(list);
    }

    [Fact]
    public async Task StockMovements_BodegueroBodega1_SoloVeFilasDeSuBodega()
    {
        var path = TempDb();
        await SeedStockMovementsAsync(path);

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([1]));
        var list = await db.StockMovements.AsNoTracking().ToListAsync();

        Assert.Equal(2, list.Count);
        Assert.Contains(list, movement => movement.Referencia == "mov-b1a");
        Assert.Contains(list, movement => movement.Referencia == "mov-b1b");
        Assert.DoesNotContain(list, movement => movement.Referencia == "mov-b2");
    }

    [Fact]
    public async Task StockMovements_BodegueroConDosBodegas_VeAmbasYNingunaOtra()
    {
        var path = TempDb();
        await SeedStockMovementsAsync(path, includeBodega3: true);

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([1, 2]));
        var list = await db.StockMovements.AsNoTracking().ToListAsync();

        Assert.Equal(3, list.Count);
        Assert.Contains(list, movement => movement.Referencia == "mov-b1a");
        Assert.Contains(list, movement => movement.Referencia == "mov-b1b");
        Assert.Contains(list, movement => movement.Referencia == "mov-b2");
        Assert.DoesNotContain(list, movement => movement.Referencia == "mov-b3");
    }

    [Fact]
    public async Task StockMovements_AccessorNull_VeTodasLasBodegas()
    {
        var path = TempDb();
        await SeedStockMovementsAsync(path, includeBodega3: true);

        await using var db = Create(path, NullCurrentPlantaInventarioAccessor.Instance);
        var list = await db.StockMovements.AsNoTracking().ToListAsync();

        Assert.Equal(4, list.Count);
        Assert.Contains(list, movement => movement.Referencia == "mov-b3");
    }

    [Fact]
    public async Task StockMovements_BodegueroSinBodega_ListaVacia()
    {
        var path = TempDb();
        await SeedStockMovementsAsync(path);

        await using var db = Create(path, new FixedCurrentPlantaInventarioAccessor([]));
        var list = await db.StockMovements.AsNoTracking().ToListAsync();

        Assert.Empty(list);
    }

    private static string TempDb() =>
        Path.Combine(Path.GetTempPath(), $"sipitex-gqf-{Guid.NewGuid():N}.db");

    private static SipitexDbContext Create(string path, ICurrentPlantaInventarioAccessor accessor) =>
        new(new DbContextOptionsBuilder<SipitexDbContext>().UseSqlite($"Data Source={path}").Options, accessor);

    private static async Task SeedMaterialsAsync(string path, bool includeBodega3 = false)
    {
        await using var db = Create(path, NullCurrentPlantaInventarioAccessor.Instance);
        await db.Database.EnsureCreatedAsync();
        if (includeBodega3)
        {
            db.PlantasInventario.Add(new PlantaInventario { Nombre = "Bodega 3" });
            await db.SaveChangesAsync();
        }
        db.Materials.AddRange(
            new Material { Code = "mat-b1a", Name = "Tela 1", Unit = MaterialUnit.Metros, Stock = 10, PlantaInventarioId = 1 },
            new Material { Code = "mat-b1b", Name = "Hilo 1", Unit = MaterialUnit.Unidades, Stock = 5, PlantaInventarioId = 1 },
            new Material { Code = "mat-b2", Name = "Forro 2", Unit = MaterialUnit.Metros, Stock = 8, PlantaInventarioId = 2 });
        if (includeBodega3)
            db.Materials.Add(new Material { Code = "mat-b3", Name = "Botón 3", Unit = MaterialUnit.Unidades, Stock = 4, PlantaInventarioId = 3 });
        await db.SaveChangesAsync();
    }

    private static async Task SeedStockMovementsAsync(string path, bool includeBodega3 = false)
    {
        await using var db = Create(path, NullCurrentPlantaInventarioAccessor.Instance);
        await db.Database.EnsureCreatedAsync();
        if (includeBodega3)
        {
            db.PlantasInventario.Add(new PlantaInventario { Nombre = "Bodega 3" });
            await db.SaveChangesAsync();
        }

        var user = new User
        {
            Nombre = "EncargadoBodega",
            Email = $"bodeguero-{Guid.NewGuid():N}@test.com",
            PasswordHash = "x",
            Rol = UserRoles.EncargadoBodega,
            IsActive = true
        };
        db.Users.Add(user);
        db.Materials.AddRange(
            new Material { Code = "mat-b1a", Name = "Tela 1", Unit = MaterialUnit.Metros, Stock = 10, PlantaInventarioId = 1 },
            new Material { Code = "mat-b1b", Name = "Hilo 1", Unit = MaterialUnit.Unidades, Stock = 5, PlantaInventarioId = 1 },
            new Material { Code = "mat-b2", Name = "Forro 2", Unit = MaterialUnit.Metros, Stock = 8, PlantaInventarioId = 2 });
        if (includeBodega3)
            db.Materials.Add(new Material { Code = "mat-b3", Name = "Boton 3", Unit = MaterialUnit.Unidades, Stock = 4, PlantaInventarioId = 3 });
        await db.SaveChangesAsync();

        var materials = await db.Materials.OrderBy(material => material.Id).ToListAsync();
        var references = new[] { "mov-b1a", "mov-b1b", "mov-b2", "mov-b3" };
        db.StockMovements.AddRange(materials.Select((material, index) => new StockMovement
        {
            MaterialId = material.Id,
            UsuarioId = user.Id,
            TipoMovimiento = StockMovementType.Entrada,
            Cantidad = 1,
            StockResultante = material.Stock,
            Referencia = references[index]
        }));
        await db.SaveChangesAsync();
    }

    private static async Task SeedSolicitudesAsync(string path, bool includeBodega3 = false)
    {
        await using var db = Create(path, NullCurrentPlantaInventarioAccessor.Instance);
        await db.Database.EnsureCreatedAsync();
        if (includeBodega3)
        {
            db.PlantasInventario.Add(new PlantaInventario { Nombre = "Bodega 3" });
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
                PlantaInventarioId = 1
            },
            new SolicitudMaterial
            {
                Codigo = "SOL-B2",
                SolicitanteId = instructor.Id,
                Estado = SolicitudMaterialEstado.Pendiente,
                PlantaInventarioId = 2
            });
        if (includeBodega3)
        {
            db.SolicitudesMaterial.Add(new SolicitudMaterial
            {
                Codigo = "SOL-B3",
                SolicitanteId = instructor.Id,
                Estado = SolicitudMaterialEstado.Pendiente,
                PlantaInventarioId = 3
            });
        }

        await db.SaveChangesAsync();
    }
}
