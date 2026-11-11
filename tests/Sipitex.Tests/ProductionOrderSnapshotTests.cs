using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class ProductionOrderSnapshotTests
{
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snapshots = new();
    private readonly Mock<IOrderMaterialRequirementRepository> _requirements = new();
    private readonly Mock<IProductionFlowRepository> _flowRepo = new();
    private readonly Mock<IProductionFlowService> _flowService = new();
    private readonly Mock<IOrderChangeLogRepository> _changeLogs = new();
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IStockMovementRepository> _stock = new();
    private readonly List<StockMovement> _movements = new();

    private ProductionOrderService CreateSut()
    {
        _requirements.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _flowService.Setup(s => s.EnsureStagesForOrderAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _flowService.Setup(s => s.LogProductionRegisteredAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (action, ct) => await action(ct));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _stock.Setup(s => s.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<StockMovement>, CancellationToken>((rows, _) => _movements.AddRange(rows))
            .Returns(Task.CompletedTask);
        return new(_orders.Object, _boms.Object, _snapshots.Object, _requirements.Object,
            _flowRepo.Object, _flowService.Object, _changeLogs.Object, _fichas.Object, _uow.Object,
            new ProductionConsumptionService(_boms.Object, _materials.Object, _stock.Object),
            NullLogger<ProductionOrderService>.Instance);
    }

    private static BomProduct EnabledProduct() => new()
    {
        Id = 1,
        ProductName = "Camisa",
        HabilitadoParaOrdenes = true,
        Items =
        [
            new BomItem
            {
                MaterialId = 1,
                Material = new Material { Id = 1, Code = "mat1", Name = "Tela Jersey", Unit = MaterialUnit.Metros, Stock = 500 },
                QuantityPerUnit = 1.6m,
                Unit = MaterialUnit.Metros,
                ProductName = "Camisa"
            }
        ]
    };

    [Fact]
    public async Task CreateOrderAsync_WhenEnabled_CreatesSnapshot()
    {
        var product = EnabledProduct();
        _boms.Setup(r => r.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _orders.Setup(r => r.NextOrderNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("OP-101");
        _orders.Setup(r => r.AddAsync(It.IsAny<ProductionOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ProductionOrder, CancellationToken>((o, _) => o.Id = 77)
            .Returns(Task.CompletedTask);

        List<ProductionOrderBomSnapshot>? saved = null;
        _snapshots.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ProductionOrderBomSnapshot>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ProductionOrderBomSnapshot>, CancellationToken>((s, _) => saved = s.ToList())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateOrderAsync(
            new CreateProductionOrderDto("Camisa", 50, DateOnly.FromDateTime(DateTime.Today.AddDays(7))));

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Single(saved!);
        Assert.Equal(77, saved![0].ProductionOrderId);
        Assert.Equal(1.6m, saved[0].QuantityPerUnit);
        Assert.Equal("Tela Jersey", saved[0].MaterialName);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenNotHabilitado_Fails()
    {
        var product = EnabledProduct();
        product.HabilitadoParaOrdenes = false;
        product.ProductName = "Blusa";
        _boms.Setup(r => r.GetProductByNameAsync("Blusa", It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await CreateSut().CreateOrderAsync(
            new CreateProductionOrderDto("Blusa", 10, DateOnly.FromDateTime(DateTime.Today)));

        Assert.False(result.Success);
        Assert.Contains("no habilitada", result.Message, StringComparison.OrdinalIgnoreCase);
        _orders.Verify(r => r.AddAsync(It.IsAny<ProductionOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterProductionAsync_UsesSnapshotNotLiveBom()
    {
        var order = new ProductionOrder
        {
            Id = 5,
            OrderNumber = "OP-105",
            ProductName = "Camisa",
            TotalQuantity = 100,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso
        };
        _orders.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Snapshot viejo: 1.0 m — BOM vivo tendría 9.9 m si se consultara
        _snapshots.Setup(r => r.GetByOrderIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderBomSnapshot
                {
                    ProductionOrderId = 5,
                    MaterialId = 1,
                    MaterialCode = "mat1",
                    MaterialName = "Tela Jersey",
                    QuantityPerUnit = 1.0m,
                    Unit = MaterialUnit.Metros
                }
            ]);

        var material = new Material { Id = 1, Name = "Tela Jersey", Stock = 50, Unit = MaterialUnit.Metros };
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(material);

        var result = await CreateSut().RegisterProductionAsync(5, 10, actorUserId: 7);

        Assert.True(result.Success);
        Assert.Equal(40m, material.Stock); // 50 - (1.0 * 10)
        Assert.Equal(10, order.ProducedQuantity);
        var salida = Assert.Single(_movements);
        Assert.Equal(StockMovementType.Salida, salida.TipoMovimiento);
        Assert.Equal(10m, salida.Cantidad);
        Assert.Equal(40m, salida.StockResultante);
        Assert.Equal("Orden:5", salida.Referencia);
        Assert.Equal(7, salida.UsuarioId);
        _boms.Verify(r => r.GetByProductAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersAsync_HintFromSnapshot()
    {
        _orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrder
                {
                    Id = 1,
                    OrderNumber = "OP-001",
                    ProductName = "Camisa",
                    TotalQuantity = 10,
                    ProducedQuantity = 0,
                    Status = OrderStatus.EnProceso,
                    Deadline = DateOnly.FromDateTime(DateTime.Today)
                }
            ]);
        _snapshots.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderBomSnapshot
                {
                    MaterialName = "Tela Jersey",
                    QuantityPerUnit = 1.6m,
                    Unit = MaterialUnit.Metros
                }
            ]);

        var list = await CreateSut().GetOrdersAsync();

        Assert.Single(list);
        Assert.Contains("Tela Jersey: 1.6", list[0].MrpHint);
        _boms.Verify(r => r.GetByProductAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
