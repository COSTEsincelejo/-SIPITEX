using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class TrazabilidadServiceTests
{
    private readonly Mock<IPrendaTrazableRepository> _prendas = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snaps = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IQualityRepository> _quality = new();
    private readonly Mock<IProductionFlowRepository> _flow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IActivityLogService> _log = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private TrazabilidadService CreateSut() => new(
        _prendas.Object, _orders.Object, _snaps.Object, _boms.Object, _consumos.Object,
        _quality.Object, _flow.Object, _users.Object, _log.Object, _uow.Object);

    private static TrazabilidadViewerFilter Admin() =>
        new(UserRoles.Administrador, 1, []);

    [Fact]
    public void PrefixFor_UsaNumeroDeOrden()
    {
        Assert.Equal("SIP-OP-001-", TrazabilidadService.PrefixFor("OP-001"));
    }

    [Fact]
    public async Task GenerarAsync_CreaCodigosConsecutivos()
    {
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder
            {
                Id = 4,
                OrderNumber = "OP-001",
                ProductName = "Polo",
                TotalQuantity = 10,
                ProducedQuantity = 3,
                EstadoProducto = EstadoProducto.Confeccion
            });
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _prendas.Setup(r => r.CountByOrderAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _prendas.Setup(r => r.GetLastCodigoForPrefixAsync("SIP-OP-001-", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        IReadOnlyList<PrendaTrazable>? saved = null;
        _prendas.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<PrendaTrazable>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<PrendaTrazable>, CancellationToken>((items, _) => saved = items)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().GenerarAsync(Admin(), 4, 2, 7);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal(new[] { "SIP-OP-001-0001", "SIP-OP-001-0002" }, saved!.Select(p => p.Codigo));
        Assert.All(saved, p => Assert.Equal("Polo", p.ProductName));
    }

    [Fact]
    public async Task GenerarAsync_NoExcedeCupoDeLaOrden()
    {
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder
            {
                Id = 4,
                OrderNumber = "OP-9",
                ProductName = "Cofia",
                TotalQuantity = 2,
                ProducedQuantity = 2
            });
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _prendas.Setup(r => r.CountByOrderAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateSut().GenerarAsync(Admin(), 4, 5, 7);

        Assert.False(result.Success);
        Assert.Contains("ya tiene", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByCodigoAsync_InstructorSinAlcance_Nulo()
    {
        _prendas.Setup(r => r.GetByCodigoAsync("SIP-OP-001-0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrendaTrazable
            {
                Id = 1,
                Codigo = "SIP-OP-001-0001",
                ProductionOrderId = 99,
                ProductName = "Polo",
                CreadoPor = new User { Id = 2, Nombre = "Otro" }
            });

        var filter = new TrazabilidadViewerFilter(UserRoles.Instructor, 10, AllowedOrderIds: [1, 2]);
        var detail = await CreateSut().GetByCodigoAsync(filter, "SIP-OP-001-0001");

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetByIdAsync_MuestraLaUnidadDelSnapshotYLaEtapaDeLaOrden()
    {
        var orden = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-001",
            ProductName = "Camisa",
            Status = OrderStatus.EnProceso,
            TotalQuantity = 120,
            ProducedQuantity = 45,
            CurrentStageId = 8
        };
        _prendas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrendaTrazable
            {
                Id = 1,
                Codigo = "SIP-OP-001-0001",
                ProductionOrderId = 1,
                ProductName = "Camisa",
                Estado = EstadoProducto.MateriaPrima,
                ProductionOrder = orden,
                CreadoPor = new User { Id = 1, Nombre = "Ana" }
            });
        _snaps.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderBomSnapshot { MaterialName = "Cremallera invisible", QuantityPerUnit = 1m, Unit = MaterialUnit.Unidades },
                new ProductionOrderBomSnapshot { MaterialName = "Hilo Poliéster", QuantityPerUnit = 18m, Unit = MaterialUnit.Metros },
                new ProductionOrderBomSnapshot { MaterialName = "Tela Jersey", QuantityPerUnit = 1.6m, Unit = MaterialUnit.Metros }
            ]);
        _consumos.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _flow.Setup(r => r.GetHistoryByOrderAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _flow.Setup(r => r.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductionOrderStage { Id = 8, Name = "Trazo", Status = ProductionStageStatus.EnProceso }]);

        var detail = await CreateSut().GetByIdAsync(Admin(), 1);

        Assert.NotNull(detail);
        Assert.Equal(
            [
                "Cremallera invisible (1 / unidad)",
                "Hilo Poliéster (18 / metro)",
                "Tela Jersey (1.6 / metro)"
            ],
            detail!.MaterialesBom);
        Assert.DoesNotContain(detail.MaterialesBom, linea => linea.Contains("/ ud", StringComparison.Ordinal));
        Assert.Equal(OrderStatus.EnProceso, detail.OrderStatus);
        Assert.Equal("Trazo", detail.EtapaActual);
        Assert.Equal(45, detail.ProducedQuantity);
        Assert.Equal(120, detail.TotalQuantity);
        _boms.Verify(r => r.GetByProductAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_SinSnapshot_UsaElBomVivoConSuUnidad()
    {
        _prendas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrendaTrazable
            {
                Id = 1,
                Codigo = "SIP-OP-009-0001",
                ProductionOrderId = 1,
                ProductName = "Camisa",
                ProductionOrder = new ProductionOrder { Id = 1, OrderNumber = "OP-009", ProductName = "Camisa" },
                CreadoPor = new User { Id = 1, Nombre = "Ana" }
            });
        _snaps.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _boms.Setup(r => r.GetByProductAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BomItem
                {
                    MaterialId = 1,
                    Material = new Material { Id = 1, Name = "Tela Jersey", Unit = MaterialUnit.Metros },
                    QuantityPerUnit = 1.6m,
                    Unit = MaterialUnit.Metros
                }
            ]);
        _consumos.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _flow.Setup(r => r.GetHistoryByOrderAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _flow.Setup(r => r.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var detail = await CreateSut().GetByIdAsync(Admin(), 1);

        Assert.Equal("Tela Jersey (1.6 / metro)", Assert.Single(detail!.MaterialesBom));
    }
}
