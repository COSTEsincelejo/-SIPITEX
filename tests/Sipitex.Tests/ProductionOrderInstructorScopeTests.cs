using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class ProductionOrderInstructorScopeTests
{
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snapshots = new();
    private readonly Mock<IOrderMaterialRequirementRepository> _requirements = new();
    private readonly Mock<IProductionFlowRepository> _flowRepo = new();
    private readonly Mock<IProductionFlowService> _flowService = new();
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMaterialRepository> _materials = new();

    private ProductionOrderService CreateSut()
    {
        _requirements.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _snapshots.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _boms.Setup(r => r.GetByProductAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _flowService.Setup(s => s.EnsureStagesForOrderAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new(
            _orders.Object,
            _boms.Object,
            _snapshots.Object,
            _requirements.Object,
            _flowRepo.Object,
            _flowService.Object,
            _fichas.Object,
            _uow.Object,
            new ProductionConsumptionService(_boms.Object, _materials.Object));
    }

    private static ProductionOrder Order(int id, string number) => new()
    {
        Id = id,
        OrderNumber = number,
        ProductName = "Camisa",
        TotalQuantity = 10,
        ProducedQuantity = 0,
        Status = OrderStatus.EnProceso,
        Deadline = DateOnly.FromDateTime(DateTime.Today)
    };

    [Fact]
    public async Task GetOrdersAsync_Instructor_OnlySeesStageAssignedOrders()
    {
        var mine = Order(1, "OP-101");
        var other = Order(2, "OP-102");
        _orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([mine, other]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage
                {
                    Id = 10,
                    ProductionOrderId = 1,
                    Name = "Corte",
                    InstructorUserId = 10
                }
            ]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage
                {
                    Id = 20,
                    ProductionOrderId = 2,
                    Name = "Corte",
                    InstructorUserId = 99
                }
            ]);
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateSut().GetOrdersAsync(10, UserRoles.Instructor, "Laura");

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.DoesNotContain(result, o => o.Id == 2);
    }

    [Fact]
    public async Task GetOrdersAsync_Instructor_SeesOrderViaFichaBelongsToInstructor()
    {
        var viaFicha = Order(3, "OP-103");
        var foreign = Order(4, "OP-104");
        _orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([viaFicha, foreign]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new Ficha
            {
                Id = 1,
                FichaCode = "F1",
                InstructorUserId = 10,
                ProductionOrderId = 3,
                Instructors = [new FichaInstructor { FichaId = 1, UserId = 10 }]
            },
            new Ficha
            {
                Id = 2,
                FichaCode = "F2",
                InstructorUserId = 20,
                ProductionOrderId = 4,
                Instructors = [new FichaInstructor { FichaId = 2, UserId = 20 }]
            }
        ]);

        var result = await CreateSut().GetOrdersAsync(10, UserRoles.Instructor, "Laura");

        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
    }

    [Fact]
    public async Task GetOrdersAsync_Administrador_SeesAllOrders()
    {
        _orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Order(1, "OP-101"), Order(2, "OP-102")]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateSut().GetOrdersAsync(1, UserRoles.Administrador, "Admin");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CanAccessOrderAsync_Instructor_CannotAccessOtherInstructorOrder()
    {
        _orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Order(1, "OP-101"), Order(2, "OP-102")]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage { ProductionOrderId = 1, Name = "Trazo", InstructorUserId = 10 }
            ]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage { ProductionOrderId = 2, Name = "Trazo", InstructorUserId = 20 }
            ]);
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = CreateSut();
        Assert.True(await sut.CanAccessOrderAsync(1, 10, UserRoles.Instructor, "Laura"));
        Assert.False(await sut.CanAccessOrderAsync(2, 10, UserRoles.Instructor, "Laura"));
    }
}
