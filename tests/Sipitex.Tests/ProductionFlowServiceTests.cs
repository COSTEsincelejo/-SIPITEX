using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class ProductionFlowServiceTests
{
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionFlowRepository> _flow = new();
    private readonly Mock<IOrderMaterialRequirementRepository> _materials = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snapshots = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMaterialRepository> _materialRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ProductionFlowService CreateSut() =>
        new(_orders.Object, _flow.Object, _materials.Object, _snapshots.Object, _boms.Object, _users.Object,
            _materialRepository.Object, _stockMovements.Object, _uow.Object);

    [Fact]
    public async Task EnsureStagesForOrder_CreatesDefaultFlowOnce()
    {
        var order = new ProductionOrder { Id = 7, ProductName = "Camisa", TotalQuantity = 100, OrderNumber = "OP-107" };
        _orders.Setup(o => o.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _flow.SetupSequence(f => f.GetStagesByOrderAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([])
            .ReturnsAsync([
                new ProductionOrderStage { Id = 1, ProductionOrderId = 7, Name = "Trazo", SortOrder = 1, QuantityReceived = 100 }
            ]);
        _flow.Setup(f => f.GetAllTemplatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _flow.Setup(f => f.GetActiveTemplateByProductAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFlowTemplate?)null);

        await CreateSut().EnsureStagesForOrderAsync(7, "Admin");

        _flow.Verify(f => f.AddStagesAsync(It.Is<IEnumerable<ProductionOrderStage>>(
            s => s.Count() == 5 && s.First().Name == "Trazo" && s.First().QuantityReceived == 100),
            It.IsAny<CancellationToken>()), Times.Once);
        _flow.Verify(f => f.AddHistoryAsync(It.IsAny<ProductionOrderHistoryEntry>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendToNext_MovesQuantityAndWritesHistory()
    {
        var from = new ProductionOrderStage
        {
            Id = 10, ProductionOrderId = 1, Name = "Trazo", SortOrder = 1,
            QuantityReceived = 50, Status = ProductionStageStatus.EnProceso
        };
        var to = new ProductionOrderStage
        {
            Id = 11, ProductionOrderId = 1, Name = "Corte", SortOrder = 2,
            QuantityReceived = 0, Status = ProductionStageStatus.Pendiente
        };
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-001",
            ProductName = "Camisa",
            Status = OrderStatus.EnProceso
        };

        _flow.Setup(f => f.GetStageByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(from);
        _flow.Setup(f => f.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([from, to]);
        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateSut().SendToNextAsync(
            new SendToNextStageDto(10, 20, null),
            actorUserId: 1, actorName: "Admin", actorRole: UserRoles.Administrador);

        Assert.True(result.Success);
        Assert.Equal(20, from.QuantitySent);
        Assert.Equal(20, to.QuantityReceived);
        Assert.Equal(ProductionStageStatus.EnProceso, to.Status);
        Assert.Equal(11, order.CurrentStageId);
        _flow.Verify(f => f.AddMovementAsync(It.IsAny<ProductionOrderStageMovement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PartialInventoryIn_IncrementsFinishedGoodStock()
    {
        var stage = new ProductionOrderStage
        {
            Id = 5, ProductionOrderId = 2, Name = "Confección", SortOrder = 3,
            QuantityReceived = 40, QuantitySent = 0, QuantityWithdrawn = 0
        };
        var order = new ProductionOrder
        {
            Id = 2, ProductName = "Camisa", TotalQuantity = 100, ProducedQuantity = 10, Status = OrderStatus.EnProceso
        };

        _flow.Setup(f => f.GetStageByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        _orders.Setup(o => o.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _flow.Setup(f => f.GetFinishedGoodAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinishedGoodStock?)null);

        var result = await CreateSut().PartialInventoryInAsync(
            new PartialInventoryInDto(2, 5, 15, "Lote A"),
            1, "Admin", UserRoles.Administrador);

        Assert.True(result.Success);
        Assert.Equal(15, stage.QuantityWithdrawn);
        Assert.Equal(25, order.ProducedQuantity);
        _flow.Verify(f => f.AddFinishedGoodAsync(It.Is<FinishedGoodStock>(s => s.Stock == 15), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InstructorWithoutPermission_CannotOperateStage()
    {
        var stage = new ProductionOrderStage
        {
            Id = 9, ProductionOrderId = 1, Name = "Corte", InstructorUserId = 99
        };
        _flow.Setup(f => f.GetStageByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, Status = OrderStatus.EnProceso, ProductName = "Camisa" });
        _flow.Setup(f => f.HasStagePermissionAsync(3, "Corte", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateSut().StartStageAsync(9, 3, "Otro", UserRoles.Instructor);

        Assert.False(result.Success);
        Assert.Contains("Sin permiso", result.Message);
    }
}
