using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Search;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Search;

namespace Sipitex.Tests;

public class BuscadorSugerenciasTests
{
    [Fact]
    public async Task Orden_LlevaAlDetalle()
    {
        await using var db = await CreateDbAsync();
        await SeedAsync(db);
        var orden = await db.ProductionOrders.SingleAsync(o => o.OrderNumber == "OP-001");

        var dto = await new BuscadorSugerenciasService(db).SugerirAsync("OP-001", Admin());

        var item = Unico(dto, "Órdenes");
        Assert.Equal("/Ordenes/Detail/" + orden.Id, item.Url);
        Assert.Contains("Entendí:", dto.Entendi, StringComparison.Ordinal);
        Assert.Contains("Detalle de la orden", item.Destino, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Material_SinAcento_EncuentraNombreConAcento_YListaCadaPlanta()
    {
        await using var db = await CreateDbAsync();
        await SeedAsync(db);

        var dto = await new BuscadorSugerenciasService(db).SugerirAsync("pantalon", Admin());
        var materiales = Items(dto, "Materiales");

        Assert.Contains(materiales, i => i.Texto.Contains("Pantalón", StringComparison.Ordinal) && i.Texto.Contains("Planta de Inventario 1", StringComparison.Ordinal));
        Assert.All(materiales, i => Assert.Contains("/PlantasInventario/Detalle/", i.Url, StringComparison.Ordinal));

        var hilo = await new BuscadorSugerenciasService(db).SugerirAsync("hilo", Admin());
        var hilos = Items(hilo, "Materiales");
        Assert.Contains(hilos, i => i.Texto.Contains("Planta de Inventario 1", StringComparison.Ordinal));
        Assert.Contains(hilos, i => i.Texto.Contains("Planta de Inventario 2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Administrador_VePersona_YElCorreoSoloSiLoBusco()
    {
        await using var db = await CreateDbAsync();
        await SeedAsync(db);
        var laura = await db.Users.SingleAsync(u => u.Email == "laura@sipitex.test");
        var sut = new BuscadorSugerenciasService(db);

        var porNombre = await sut.SugerirAsync("instructor laura", Admin());
        var persona = Unico(porNombre, "Personas");
        Assert.Equal("/Account/EditUser/" + laura.Id, persona.Url);
        Assert.DoesNotContain("@", persona.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", persona.Texto, StringComparison.Ordinal);

        var porCorreo = await sut.SugerirAsync("laura@sipitex.test", Admin());
        var conCorreo = Unico(porCorreo, "Personas");
        Assert.Contains("laura@sipitex.test", conCorreo.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Encargado_NoRecibePlantasAjenas()
    {
        var accessor = new FixedCurrentPlantaInventarioAccessor([1]);
        await using var db = await CreateDbAsync(accessor);
        await SeedAsync(db);
        var sut = new BuscadorSugerenciasService(db);
        var alcance = Encargado();

        var boton = await sut.SugerirAsync("boton", alcance);
        Assert.DoesNotContain(Todos(boton), i => i.Url.Contains("/Detalle/2", StringComparison.Ordinal) || i.Texto.Contains("Botón", StringComparison.Ordinal));

        var planta2 = await sut.SugerirAsync("planta 2", alcance);
        Assert.DoesNotContain(Todos(planta2), i => i.Url.Contains("/2", StringComparison.Ordinal));

        var solicitud = await sut.SugerirAsync("SOL-0008", alcance);
        Assert.DoesNotContain(Todos(solicitud), i => i.Texto.Contains("SOL-0008", StringComparison.Ordinal));
        var propia = await sut.SugerirAsync("SOL-0007", alcance);
        Assert.Contains(Todos(propia), i => i.Texto.Contains("SOL-0007", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Instructor_NoRecibeUsuariosNiInventarioOperativo()
    {
        await using var db = await CreateDbAsync();
        await SeedAsync(db);
        var laura = await db.Users.SingleAsync(u => u.Email == "laura@sipitex.test");
        var sut = new BuscadorSugerenciasService(db);
        var alcance = Instructor(laura.Id);

        var persona = await sut.SugerirAsync("laura@sipitex.test", alcance);
        Assert.Equal("", persona.Entendi);
        Assert.DoesNotContain(Todos(persona), i =>
            i.Url.Contains("/Account/", StringComparison.Ordinal)
            || i.Texto.Contains("@", StringComparison.Ordinal)
            || i.Url.Contains("/PlantasInventario/", StringComparison.Ordinal));

        var tela = await sut.SugerirAsync("tela jersey", alcance);
        Assert.DoesNotContain(Todos(tela), i =>
            i.Url.Contains("/PlantasInventario/", StringComparison.Ordinal)
            || i.Url.Contains("/Account/", StringComparison.Ordinal));

        var pendientes = await sut.SugerirAsync("solicitudes pendientes", alcance);
        Assert.Contains(Todos(pendientes), i => i.Texto.Contains("SOL-0007", StringComparison.Ordinal));
        Assert.DoesNotContain(Todos(pendientes), i => i.Texto.Contains("SOL-0099", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Limite_CincoPorGrupo_YVeinteEnTotal()
    {
        await using var db = await CreateDbAsync();
        await SeedTopeAsync(db);

        var dto = await new BuscadorSugerenciasService(db).SugerirAsync("rollo", Admin());
        var total = dto.Grupos.Sum(g => g.Items.Count);

        Assert.InRange(total, 1, 20);
        Assert.All(dto.Grupos, g => Assert.InRange(g.Items.Count, 1, 5));
        Assert.Equal(5, Items(dto, "Materiales").Count);
        Assert.True(total <= 20);
    }

    [Fact]
    public async Task ConsultaCorta_DevuelveVacio()
    {
        await using var db = await CreateDbAsync();
        await SeedAsync(db);

        var dto = await new BuscadorSugerenciasService(db).SugerirAsync("a", Admin());

        Assert.Equal("", dto.Entendi);
        Assert.Empty(dto.Grupos);
        Assert.Empty(dto.Ejemplos);
    }

    [Fact]
    public async Task StockCritico_UsaLaPlantaDeInventario()
    {
        await using var db = await CreateDbAsync();
        await SeedAsync(db);

        var dto = await new BuscadorSugerenciasService(db).SugerirAsync("crítico planta 1", Admin());

        Assert.Contains("planta de inventario", dto.Entendi, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bodega", dto.Entendi, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Todos(dto), i => i.Url.Contains("nivel=Critico", StringComparison.Ordinal));
        Assert.DoesNotContain(Todos(dto), i => i.Texto.Contains("@", StringComparison.Ordinal));
    }

    private static async Task<SipitexDbContext> CreateDbAsync(ICurrentPlantaInventarioAccessor? accessor = null)
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<SipitexDbContext>().UseSqlite(conn).Options;
        var db = accessor is null
            ? new SipitexDbContext(options)
            : new SipitexDbContext(options, accessor);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task SeedAsync(SipitexDbContext db)
    {
        var laura = new User
        {
            Nombre = "Laura Gómez",
            Email = "laura@sipitex.test",
            PasswordHash = "hash",
            Rol = UserRoles.Instructor,
            IsActive = true
        };
        var otro = new User
        {
            Nombre = "Pedro Ruiz",
            Email = "pedro@sipitex.test",
            PasswordHash = "hash",
            Rol = UserRoles.Instructor,
            IsActive = true
        };
        db.Users.AddRange(laura, otro);
        await db.SaveChangesAsync();

        db.Materials.AddRange(
            new Material { Name = "Tela jersey", Code = "mat-jersey", Unit = MaterialUnit.Metros, Stock = 0, MinStock = 5, PlantaInventarioId = 1 },
            new Material { Name = "Pantalón", Code = "mat-pan", Unit = MaterialUnit.Metros, Stock = 4, MinStock = 0, PlantaInventarioId = 1 },
            new Material { Name = "Botón nácar", Code = "mat-boton", Unit = MaterialUnit.Unidades, Stock = 8, MinStock = 2, PlantaInventarioId = 2 },
            new Material { Name = "Hilo", Code = "mat-hilo-1", Unit = MaterialUnit.Unidades, Stock = 6, MinStock = 1, PlantaInventarioId = 1 },
            new Material { Name = "Hilo", Code = "mat-hilo-2", Unit = MaterialUnit.Unidades, Stock = 6, MinStock = 1, PlantaInventarioId = 2 });
        db.ProductionOrders.Add(new ProductionOrder
        {
            OrderNumber = "OP-001",
            ProductName = "Camisa",
            TotalQuantity = 10,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso,
            Deadline = new DateOnly(2026, 12, 1)
        });
        db.BomProducts.Add(new BomProduct { ProductName = "Camisa", Codigo = "PRD-0001" });
        db.SolicitudesMaterial.AddRange(
            new SolicitudMaterial { Codigo = "SOL-0007", SolicitanteId = laura.Id, Estado = SolicitudMaterialEstado.Pendiente, PlantaInventarioId = 1 },
            new SolicitudMaterial { Codigo = "SOL-0008", SolicitanteId = laura.Id, Estado = SolicitudMaterialEstado.Pendiente, PlantaInventarioId = 2 },
            new SolicitudMaterial { Codigo = "SOL-0099", SolicitanteId = otro.Id, Estado = SolicitudMaterialEstado.Pendiente, PlantaInventarioId = 1 });
        await db.SaveChangesAsync();
    }

    private static async Task SeedTopeAsync(SipitexDbContext db)
    {
        var laura = new User
        {
            Nombre = "Laura Gómez",
            Email = "laura@sipitex.test",
            PasswordHash = "hash",
            Rol = UserRoles.Instructor,
            IsActive = true
        };
        db.Users.Add(laura);
        await db.SaveChangesAsync();

        for (var i = 1; i <= 8; i++)
        {
            db.Materials.Add(new Material
            {
                Name = "Rollo tela " + i,
                Code = "mat-rollo-" + i,
                Unit = MaterialUnit.Metros,
                Stock = 10,
                MinStock = 1,
                PlantaInventarioId = 1
            });
        }

        for (var i = 1; i <= 6; i++)
        {
            db.ProductionOrders.Add(new ProductionOrder
            {
                OrderNumber = "OP-20" + i,
                ProductName = "Rollo especial " + i,
                TotalQuantity = 10,
                ProducedQuantity = 0,
                Status = OrderStatus.EnProceso,
                Deadline = new DateOnly(2026, 12, 1)
            });
            db.BomProducts.Add(new BomProduct { ProductName = "Rollo ficha " + i, Codigo = "PRD-R0" + i });
            db.SolicitudesMaterial.Add(new SolicitudMaterial
            {
                Codigo = "SOL-ROLLO-" + i,
                SolicitanteId = laura.Id,
                Estado = SolicitudMaterialEstado.Pendiente,
                PlantaInventarioId = 1
            });
        }

        await db.SaveChangesAsync();
        var ordenes = await db.ProductionOrders.OrderBy(o => o.Id).ToListAsync();
        foreach (var orden in ordenes)
        {
            db.GruposConfeccion.Add(new GrupoConfeccion
            {
                ProductionOrderId = orden.Id,
                InstructorUserId = laura.Id,
                CantidadPrendas = 4,
                FechaRealizacion = new DateOnly(2026, 6, 1)
            });
        }

        await db.SaveChangesAsync();
    }

    private static AlcanceBusqueda Admin() => new()
    {
        Rol = UserRoles.Administrador,
        UserId = 1,
        PuedeVerInventario = true,
        PuedeVerUsuarios = true,
        PuedeVerGrupos = true,
        PuedeVerMovimientos = true,
        PuedeVerReingreso = true,
        PuedeVerTodasLasFichasTecnicas = true
    };

    private static AlcanceBusqueda Encargado() => new()
    {
        Rol = UserRoles.EncargadoDeBodega,
        UserId = 2,
        PlantaInventarioIds = [1],
        PuedeVerInventario = true,
        PuedeVerUsuarios = false,
        PuedeVerGrupos = false,
        PuedeVerMovimientos = true,
        PuedeVerReingreso = true
    };

    private static AlcanceBusqueda Instructor(int userId) => new()
    {
        Rol = UserRoles.Instructor,
        UserId = userId,
        EsInstructor = true,
        PuedeVerInventario = false,
        PuedeVerUsuarios = false,
        PuedeVerGrupos = true,
        PuedeVerTodasLasFichasTecnicas = false
    };

    private static IEnumerable<ItemSugerenciaDto> Todos(SugerenciasBusquedaDto dto) =>
        dto.Grupos.SelectMany(g => g.Items);

    private static List<ItemSugerenciaDto> Items(SugerenciasBusquedaDto dto, string tipo) =>
        dto.Grupos.Single(g => g.Tipo == tipo).Items.ToList();

    private static ItemSugerenciaDto Unico(SugerenciasBusquedaDto dto, string tipo)
    {
        var items = Items(dto, tipo);
        return Assert.Single(items);
    }
}
