using Microsoft.EntityFrameworkCore;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;
using Sipitex.Infrastructure.Reporting;
using Sipitex.Infrastructure.Repositories;
using Sipitex.Infrastructure.Services;

namespace Sipitex.Tests;

public class ActaEstadosTests
{
    private readonly Mock<IActaMovimientoRepository> _actas = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPlantaInventarioRepository> _plantas = new();
    private readonly Mock<IActivityLogService> _activity = new();

    public ActaEstadosTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _actas.Setup(r => r.GetLastNumeroAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _actas.Setup(r => r.GetNumerosPorStockMovementAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
        _actas.Setup(r => r.GetNumerosPorConsumoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
    }

    private ActaMovimientoService CreateSut() => new(
        _actas.Object,
        Mock.Of<IStockMovementRepository>(),
        Mock.Of<IConsumoMaterialRepository>(),
        Mock.Of<IProductionOrderRepository>(),
        _users.Object,
        new ActaPdfService(),
        _uow.Object,
        plantas: _plantas.Object,
        activityLog: _activity.Object);

    [Fact]
    public async Task CreateAsync_SinFirmaDeRecibe_QuedaPendienteFirma()
    {
        ActaMovimiento? saved = null;
        _actas.Setup(r => r.AddAsync(It.IsAny<ActaMovimiento>(), It.IsAny<CancellationToken>()))
            .Callback<ActaMovimiento, CancellationToken>((a, _) =>
            {
                a.Id = 4;
                saved = a;
            })
            .Returns(Task.CompletedTask);
        _actas.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(() => saved);

        var result = await CreateSut().CreateAsync(new CreateActaDto(
            ActaTipo.Ingreso, ActaOrigen.Manual,
            "Pedro", "Encargado", true,
            "Laura", "Instructor", true,
            7, Observaciones: "Falta la firma de recibe",
            EntregaFirmaPng: TestPng.Bytes));

        Assert.True(result.Success, result.Message);
        Assert.Equal(ActaEstado.PendienteFirma, result.Value!.Estado);
        Assert.Null(result.Value.RecibeConformidadUtc);
        Assert.NotNull(result.Value.EntregaConformidadUtc);
    }

    [Fact]
    public async Task FirmarRecibeAsync_CompletaElActaYRegistraLaConformidad()
    {
        var acta = new ActaMovimiento
        {
            Id = 4,
            Numero = "ACT-0004",
            Estado = ActaEstado.PendienteFirma,
            CreadoPorUserId = 7,
            EntregaNombre = "Pedro",
            RecibeNombre = "Laura"
        };
        _actas.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(acta);

        var result = await CreateSut().FirmarRecibeAsync(4, 7, UserRoles.Instructor, true, TestPng.Bytes);

        Assert.True(result.Success, result.Message);
        Assert.Equal(ActaEstado.Firmada, acta.Estado);
        Assert.NotNull(acta.RecibeConformidadUtc);
        Assert.Equal(TestPng.Bytes, acta.RecibeFirmaPng);
        _activity.Verify(a => a.LogAsync(
            7,
            ActivityLogActions.FirmarActaRecibe,
            ActivityLogEntities.ActaMovimiento,
            "4",
            It.Is<string>(d => d.Contains("ACT-0004", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnularAsync_ExigeMotivoYSoloAdministrador()
    {
        var acta = new ActaMovimiento
        {
            Id = 5,
            Numero = "ACT-0005",
            Estado = ActaEstado.Firmada,
            EntregaNombre = "Pedro",
            RecibeNombre = "Laura"
        };
        _actas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(acta);

        var ajena = await CreateSut().AnularAsync(5, 8, UserRoles.Instructor, "Motivo suficiente para anular");
        Assert.False(ajena.Success);
        Assert.Contains("administrador", ajena.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ActaEstado.Firmada, acta.Estado);

        var corta = await CreateSut().AnularAsync(5, 1, UserRoles.Administrador, "corto");
        Assert.False(corta.Success);
        Assert.Contains("10", corta.Message, StringComparison.Ordinal);

        var ok = await CreateSut().AnularAsync(5, 1, UserRoles.Administrador, "Destinatario equivocado en el acta");
        Assert.True(ok.Success, ok.Message);
        Assert.Equal(ActaEstado.Anulada, acta.Estado);
        Assert.Equal(1, acta.AnuladaPorUserId);
        Assert.NotNull(acta.AnuladaUtc);
        Assert.Equal("Destinatario equivocado en el acta", acta.MotivoAnulacion);

        var otra = await CreateSut().FirmarRecibeAsync(5, 1, UserRoles.Administrador, true, TestPng.Bytes);
        Assert.False(otra.Success);
        Assert.Contains("anulada", otra.Message, StringComparison.OrdinalIgnoreCase);

        _activity.Verify(a => a.LogAsync(
            1,
            ActivityLogActions.AnularActa,
            ActivityLogEntities.ActaMovimiento,
            "5",
            It.Is<string>(d => d.Contains("Destinatario equivocado", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_StockSinPlantas_Falla()
    {
        var result = await CreateSut().CreateAsync(new CreateActaDto(
            ActaTipo.Ingreso, ActaOrigen.Stock,
            "Pedro", "Encargado", true,
            "Laura", "Instructor", true,
            7,
            EntregaFirmaPng: TestPng.Bytes,
            StockMovementIds: [9]));

        Assert.False(result.Success);
        Assert.Contains("planta", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_PlantaInactiva_Falla()
    {
        _plantas.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlantaInventario { Id = 1, Nombre = "Uno", Activo = true });
        _plantas.Setup(p => p.GetByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlantaInventario { Id = 9, Nombre = "Cerrada", Activo = false });

        var result = await CreateSut().CreateAsync(new CreateActaDto(
            ActaTipo.Ingreso, ActaOrigen.Stock,
            "Pedro", "Encargado", true,
            "Laura", "Instructor", true,
            7,
            EntregaFirmaPng: TestPng.Bytes,
            RecibeFirmaPng: TestPng.Bytes,
            PlantaOrigenId: 1,
            PlantaDestinoId: 9,
            StockMovementIds: [9]));

        Assert.False(result.Success);
        Assert.Contains("destino", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}

public class ActaAnulacionRepositorioTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"sipitex-actas-{Guid.NewGuid():N}.db");
    private readonly SipitexDbContext _db;

    public ActaAnulacionRepositorioTests()
    {
        var options = new DbContextOptionsBuilder<SipitexDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        _db = new SipitexDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public async Task ActaAntigua_SinColumnaDeEstado_QuedaFirmada()
    {
        _db.Users.Add(new User
        {
            Nombre = "Ana",
            Email = "ana-actas@sipitex.test",
            PasswordHash = "x",
            Rol = UserRoles.Administrador
        });
        await _db.SaveChangesAsync();
        var userId = _db.Users.Single().Id;

        await _db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO ActasMovimiento
                (Numero, Tipo, Origen, FechaUtc, EntregaNombre, EntregaCargo, RecibeNombre, RecibeCargo, CreadoPorUserId)
            VALUES
                ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})
            """,
            "ACT-VIEJA", "Ingreso", "Manual", DateTime.UtcNow, "Ana", "Admin", "Pedro", "Bodega", userId);

        var vieja = await _db.ActasMovimiento.AsNoTracking().SingleAsync(a => a.Numero == "ACT-VIEJA");
        Assert.Equal(ActaEstado.Firmada, vieja.Estado);
    }

    [Fact]
    public async Task Anular_LiberaMovimientosYConsumosParaOtraActa()
    {
        var user = new User
        {
            Nombre = "Admin",
            Email = "admin-actas@sipitex.test",
            PasswordHash = "x",
            Rol = UserRoles.Administrador
        };
        _db.Users.Add(user);
        var material = new Material { Name = "Tela", Code = "MAT-ACTA", PlantaInventarioId = 1 };
        var orden = new ProductionOrder { OrderNumber = "OP-ACTA", ProductName = "Camisa", TotalQuantity = 1 };
        _db.Materials.Add(material);
        _db.ProductionOrders.Add(orden);
        await _db.SaveChangesAsync();

        var movimiento = new StockMovement
        {
            MaterialId = material.Id,
            UsuarioId = user.Id,
            TipoMovimiento = StockMovementType.Entrada,
            Cantidad = 3,
            StockResultante = 3
        };
        var consumo = new ConsumoMaterial
        {
            ProductionOrderId = orden.Id,
            MaterialId = material.Id,
            Cantidad = 1,
            FechaUtc = DateTime.UtcNow,
            ResponsableUserId = user.Id
        };
        _db.StockMovements.Add(movimiento);
        _db.ConsumosMaterial.Add(consumo);
        await _db.SaveChangesAsync();

        var acta = new ActaMovimiento
        {
            Numero = "ACT-0008",
            Tipo = ActaTipo.Egreso,
            Origen = ActaOrigen.Stock,
            FechaUtc = DateTime.UtcNow,
            Estado = ActaEstado.Firmada,
            EntregaNombre = "Ana",
            RecibeNombre = "Pedro",
            CreadoPorUserId = user.Id,
            Detalles =
            [
                new ActaMovimientoDetalle
                {
                    ItemTipo = ActaItemTipo.Material,
                    Descripcion = "Tela",
                    Cantidad = 3,
                    StockMovementId = movimiento.Id,
                    ConsumoMaterialId = consumo.Id,
                    MaterialId = material.Id
                }
            ]
        };
        _db.ActasMovimiento.Add(acta);
        await _db.SaveChangesAsync();

        var repo = new ActaMovimientoRepository(_db);
        var ocupadosAntes = await repo.GetNumerosPorStockMovementAsync();
        var consumosAntes = await repo.GetNumerosPorConsumoAsync();
        Assert.Equal("ACT-0008", ocupadosAntes[movimiento.Id]);
        Assert.Equal("ACT-0008", consumosAntes[consumo.Id]);

        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var service = new ActaMovimientoService(
            repo,
            Mock.Of<IStockMovementRepository>(),
            Mock.Of<IConsumoMaterialRepository>(),
            Mock.Of<IProductionOrderRepository>(),
            users.Object,
            new ActaPdfService(),
            new UnitOfWork(_db),
            activityLog: new ActivityLogService(_db, users.Object));

        var result = await service.AnularAsync(acta.Id, user.Id, UserRoles.Administrador, "Se registró el destinatario incorrecto");
        Assert.True(result.Success, result.Message);

        var ocupados = await repo.GetNumerosPorStockMovementAsync();
        var consumos = await repo.GetNumerosPorConsumoAsync();
        Assert.False(ocupados.ContainsKey(movimiento.Id));
        Assert.False(consumos.ContainsKey(consumo.Id));

        var log = Assert.Single(await _db.ActivityLogs.AsNoTracking().ToListAsync());
        Assert.Equal(ActivityLogActions.AnularActa, log.Action);
        Assert.Contains("destinatario incorrecto", log.Details, StringComparison.OrdinalIgnoreCase);

        var firma = await service.FirmarRecibeAsync(acta.Id, user.Id, UserRoles.Administrador, true, TestPng.Bytes);
        Assert.False(firma.Success);
    }
}
