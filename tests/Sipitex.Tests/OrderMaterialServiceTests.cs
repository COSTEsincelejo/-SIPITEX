using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class OrderMaterialServiceTests
{
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IOrderMaterialRequirementRepository> _reqs = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snapshots = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private OrderMaterialService CreateSut()
    {
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (action, ct) => await action(ct));
        return new(_orders.Object, _reqs.Object, _materials.Object, _snapshots.Object, _stockMovements.Object, _uow.Object);
    }

    [Fact]
    public async Task AddMaterialAsync_SetsPendienteRevisionBodega()
    {
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-101",
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.NoAplica
        };
        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _materials.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 5, Name = "Hilo", Unit = MaterialUnit.Metros, Stock = 100 });
        _reqs.Setup(r => r.ExistsAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateSut().AddMaterialAsync(new AddOrderMaterialDto(1, 5, 12, null));

        Assert.True(result.Success);
        Assert.Equal(OrderMaterialsStatus.PendienteRevisionBodega, order.MaterialsStatus);
        _reqs.Verify(r => r.AddAsync(It.Is<ProductionOrderMaterialRequirement>(
            l => l.MaterialId == 5 && l.QuantityRequired == 12), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_Partial_UpdatesStockAndStatus()
    {
        var material = new Material { Id = 5, Name = "Hilo", Unit = MaterialUnit.Metros, Stock = 10 };
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-101",
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega
        };
        var line = new ProductionOrderMaterialRequirement
        {
            Id = 9,
            ProductionOrderId = 1,
            MaterialId = 5,
            Material = material,
            QuantityRequired = 20,
            QuantityDelivered = 0,
            Unit = MaterialUnit.Metros
        };

        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _reqs.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([line]);

        var result = await CreateSut().DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 10)], null),
            bodegueroId: 3);

        Assert.True(result.Success);
        Assert.Equal(0m, material.Stock);
        Assert.Equal(10m, line.QuantityDelivered);
        Assert.Equal(OrderMaterialsStatus.EntregaParcial, order.MaterialsStatus);
        Assert.Contains("Pendientes", result.Message);
    }

    [Fact]
    public async Task DeliverAsync_Full_SetsListaParaProduccion()
    {
        var material = new Material { Id = 5, Name = "Hilo", Unit = MaterialUnit.Metros, Stock = 50 };
        var order = new ProductionOrder
        {
            Id = 1,
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.MaterialesValidados
        };
        var line = new ProductionOrderMaterialRequirement
        {
            Id = 9,
            ProductionOrderId = 1,
            MaterialId = 5,
            Material = material,
            QuantityRequired = 20,
            QuantityDelivered = 0,
            Unit = MaterialUnit.Metros
        };
        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _reqs.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([line]);

        var result = await CreateSut().DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 20)], null),
            3);

        Assert.True(result.Success);
        Assert.Equal(30m, material.Stock);
        Assert.Equal(OrderMaterialsStatus.ListaParaProduccion, order.MaterialsStatus);
    }

    [Fact]
    public async Task DeliverAsync_NeverAllowsNegativeStock()
    {
        var material = new Material { Id = 5, Name = "Hilo", Stock = 3, Unit = MaterialUnit.Metros };
        var order = new ProductionOrder { Id = 1, Status = OrderStatus.EnProceso, MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega };
        var line = new ProductionOrderMaterialRequirement
        {
            Id = 9, ProductionOrderId = 1, MaterialId = 5, Material = material,
            QuantityRequired = 10, QuantityDelivered = 0, Unit = MaterialUnit.Metros
        };
        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _reqs.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);

        var result = await CreateSut().DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 10)], null),
            3);

        Assert.False(result.Success);
        Assert.Equal(3m, material.Stock);
        Assert.Equal(0m, line.QuantityDelivered);
    }

    [Fact]
    public async Task RegisterProduction_BlockedWhenMaterialsPending()
    {
        var orders = new Mock<IProductionOrderRepository>();
        var boms = new Mock<IBomRepository>();
        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        var reqs = new Mock<IOrderMaterialRequirementRepository>();
        var flowRepo = new Mock<IProductionFlowRepository>();
        var flowService = new Mock<IProductionFlowService>();
        var uow = new Mock<IUnitOfWork>();
        var materials = new Mock<IMaterialRepository>();
        reqs.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        flowRepo.Setup(r => r.GetStagesByOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var order = new ProductionOrder
        {
            Id = 2,
            TotalQuantity = 100,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega
        };
        orders.Setup(o => o.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = new ProductionOrderService(
            orders.Object, boms.Object, snapshots.Object, reqs.Object,
            flowRepo.Object, flowService.Object, uow.Object,
            new ProductionConsumptionService(boms.Object, materials.Object));

        var result = await sut.RegisterProductionAsync(2, 5);

        Assert.False(result.Success);
        Assert.Contains("pendientes de entrega", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, order.ProducedQuantity);
    }
}
