using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Repositories;

namespace Sipitex.Tests;

public class MaterialPlantillaCmtcTests
{
    [Fact]
    public async Task AddMaterialAsync_PersisteCamposDeLaPlantilla()
    {
        await using var db = await CreateDbAsync();
        var service = CreateService(db);

        var result = await service.AddMaterialAsync(
            new CreateMaterialDto(
                "Tela jersey",
                12m,
                MaterialUnit.Rollo,
                StockEntryOrigin.Compra,
                4.5m,
                1,
                "ALM-100",
                "REF-JERSEY",
                "Telas",
                "Punto",
                "Rollo de jersey 100% algodón",
                "Textiles del Sinú",
                TipoElemento.Devolutivo),
            actorUserId: 7);

        Assert.True(result.Success, result.Message);
        var material = Assert.Single(await db.Materials.ToListAsync());
        Assert.Equal("ALM-100", material.CodigoAlmacen);
        Assert.Equal("REF-JERSEY", material.Referencia);
        Assert.Equal("Telas", material.Categoria);
        Assert.Equal("Punto", material.Subcategoria);
        Assert.Equal("Rollo de jersey 100% algodón", material.Descripcion);
        Assert.Equal("Textiles del Sinú", material.Proveedor);
        Assert.Equal(TipoElemento.Devolutivo, material.TipoElemento);
        Assert.Equal(MaterialUnit.Rollo, material.Unit);
        Assert.Matches(@"^mat\d+$", material.Code);
        Assert.NotEqual(material.CodigoAlmacen, material.Code);
    }

    [Fact]
    public async Task DosMateriales_PuedenCompartirCodigoAlmacen()
    {
        await using var db = await CreateDbAsync();
        db.Materials.AddRange(
            Nuevo("mat-a", "ALM-100", "Tela A"),
            Nuevo("mat-b", "ALM-100", "Tela B"));

        await db.SaveChangesAsync();

        var rows = await db.Materials.OrderBy(m => m.Code).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, m => Assert.Equal("ALM-100", m.CodigoAlmacen));
        Assert.NotEqual(rows[0].Code, rows[1].Code);
    }

    [Fact]
    public async Task CodeAutogenerado_SigueSiendoUnico()
    {
        await using var db = await CreateDbAsync();
        var service = CreateService(db);
        var dto = new CreateMaterialDto(
            "Hilo",
            1m,
            MaterialUnit.Cono,
            StockEntryOrigin.Compra,
            CodigoAlmacen: "ALM-100");

        Assert.True((await service.AddMaterialAsync(dto, 3)).Success);
        await WaitUntilClockMovesAsync();
        Assert.True((await service.AddMaterialAsync(dto with { Name = "Hilo 2" }, 3)).Success);

        var codes = await db.Materials.Select(m => m.Code).ToListAsync();
        Assert.Equal(2, codes.Count);
        Assert.Equal(codes.Distinct().Count(), codes.Count);
        Assert.All(codes, code => Assert.Matches(@"^mat\d+$", code));

        db.Materials.Add(Nuevo(codes[0], "ALM-OTRO", "Duplicado"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task IndiceDeCodigoAlmacen_NoEsUnico_YCodeSi()
    {
        await using var db = await CreateDbAsync();
        var entity = db.Model.FindEntityType(typeof(Material));
        Assert.NotNull(entity);

        var almacen = entity!.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(Material.CodigoAlmacen));
        Assert.False(almacen.IsUnique);

        var code = entity.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(Material.Code));
        Assert.True(code.IsUnique);
    }

    [Fact]
    public async Task UpdateMaterialAsync_ActualizaCamposDeLaPlantilla_SinTocarCodeNiStock()
    {
        await using var db = await CreateDbAsync();
        var material = Nuevo("mat-edit", null, "Tela vieja");
        material.Stock = 40;
        material.Unit = MaterialUnit.Metros;
        db.Materials.Add(material);
        await db.SaveChangesAsync();

        var result = await CreateService(db).UpdateMaterialAsync(new UpdateMaterialDto(
            material.Id,
            "Tela jersey premium",
            MaterialUnit.Paquete,
            5,
            9m,
            "ALM-9",
            "REF-9",
            "Avíos",
            "Botones",
            "Paquete de botones",
            "Proveedor 9",
            TipoElemento.Consumible));

        Assert.True(result.Success, result.Message);
        var saved = await db.Materials.SingleAsync();
        Assert.Equal("mat-edit", saved.Code);
        Assert.Equal(40m, saved.Stock);
        Assert.Equal("ALM-9", saved.CodigoAlmacen);
        Assert.Equal("REF-9", saved.Referencia);
        Assert.Equal("Avíos", saved.Categoria);
        Assert.Equal("Botones", saved.Subcategoria);
        Assert.Equal("Paquete de botones", saved.Descripcion);
        Assert.Equal("Proveedor 9", saved.Proveedor);
        Assert.Equal(TipoElemento.Consumible, saved.TipoElemento);
        Assert.Equal(MaterialUnit.Paquete, saved.Unit);
    }

    [Fact]
    public void PlantaDetalleConsulta_FiltraPorCategoriaYTipoElemento()
    {
        var items = new List<MaterialPlantaStockDto>
        {
            Fila("Tela", "Telas", TipoElemento.Devolutivo),
            Fila("Hilo", "Hilos", TipoElemento.Consumible),
            Fila("Botón", "Avíos", TipoElemento.Consumible)
        };

        var telas = PlantaDetalleConsulta.Apply(items, null, null, "telas", null);
        Assert.Equal("Tela", Assert.Single(telas).Name);

        var consumibles = PlantaDetalleConsulta.Apply(items, null, null, null, TipoElemento.Consumible);
        Assert.Equal(["Botón", "Hilo"], consumibles.Select(m => m.Name).ToArray());
    }

    [Fact]
    public void InventarioConsultaFilter_FiltraPorCategoriaYTipoElemento()
    {
        var items = new List<MaterialDto>
        {
            Consulta("Tela", "Telas", TipoElemento.Devolutivo),
            Consulta("Hilo", "Hilos", TipoElemento.Consumible)
        };

        var telas = InventarioConsultaFilter.Apply(items, null, null, "TELAS", null);
        Assert.Equal("Tela", Assert.Single(telas).Name);

        var consumibles = InventarioConsultaFilter.Apply(items, null, null, null, TipoElemento.Consumible);
        Assert.Equal("Hilo", Assert.Single(consumibles).Name);
    }

    private static Material Nuevo(string code, string? codigoAlmacen, string name) => new()
    {
        Code = code,
        CodigoAlmacen = codigoAlmacen,
        Name = name,
        Unit = MaterialUnit.Metros,
        PlantaInventarioId = 1
    };

    private static MaterialPlantaStockDto Fila(string name, string categoria, TipoElemento tipo) =>
        new(name.ToLowerInvariant(), name, MaterialUnit.Metros, 1, 1, false, Categoria: categoria, TipoElemento: tipo);

    private static MaterialDto Consulta(string name, string categoria, TipoElemento tipo) =>
        new(1, name, "m", MaterialUnit.Metros, 1, MaterialStatus.Bueno, 1, false, new DateOnly(2026, 1, 1),
            Categoria: categoria, TipoElemento: tipo);

    private static InventoryService CreateService(SipitexDbContext db) => new(
        new MaterialRepository(db),
        MockRequestRepo(),
        MockOrderRepo(),
        MockBomRepo(),
        MockMovementRepo(),
        new UnitOfWork(db));

    private static Application.Interfaces.Repositories.IMaterialRequestRepository MockRequestRepo() =>
        new Moq.Mock<Application.Interfaces.Repositories.IMaterialRequestRepository>().Object;

    private static Application.Interfaces.Repositories.IProductionOrderRepository MockOrderRepo() =>
        new Moq.Mock<Application.Interfaces.Repositories.IProductionOrderRepository>().Object;

    private static Application.Interfaces.Repositories.IBomRepository MockBomRepo() =>
        new Moq.Mock<Application.Interfaces.Repositories.IBomRepository>().Object;

    private static Application.Interfaces.Repositories.IStockMovementRepository MockMovementRepo()
    {
        var mock = new Moq.Mock<Application.Interfaces.Repositories.IStockMovementRepository>();
        mock.Setup(r => r.AddAsync(Moq.It.IsAny<StockMovement>(), Moq.It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock.Object;
    }

    private static async Task<SipitexDbContext> CreateDbAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sipitex-plantilla-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        var db = new SipitexDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task WaitUntilClockMovesAsync()
    {
        var start = DateTime.UtcNow.Ticks;
        for (var i = 0; i < 40 && DateTime.UtcNow.Ticks == start; i++)
            await Task.Delay(5);
    }
}
