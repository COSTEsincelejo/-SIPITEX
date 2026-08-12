using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

/// <summary>
/// Gate de materiales: BomProductInstructor ∪ etapa MES; independiente de CanAccessOrderAsync (MES/producción).
/// </summary>
public class OrderMaterialsBomInstructorGateTests
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

    private ProductionOrderService CreateSut()
    {
        _requirements.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _fichas.Setup(f => f.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return new(_orders.Object, _boms.Object, _snapshots.Object, _requirements.Object,
            _flowRepo.Object, _flowService.Object, _changeLogs.Object, _fichas.Object, _uow.Object,
            new ProductionConsumptionService(_boms.Object, _materials.Object));
    }

    [Fact]
    public async Task AuthorizeOrderMaterials_BomInstructorNotOnStage_Succeeds()
    {
        var sut = CreateSut();
        _orders.Setup(o => o.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 5, ProductName = "Camisa", Status = OrderStatus.EnProceso });
        _flowRepo.Setup(f => f.GetStagesByOrderAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage { Id = 1, ProductionOrderId = 5, InstructorUserId = 99, Name = "Trazo" }
            ]);
        _boms.Setup(b => b.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BomProduct
            {
                ProductName = "Camisa",
                Instructors = [new BomProductInstructor { UserId = 10, BomProductId = 1 }]
            });

        var result = await sut.AuthorizeOrderMaterialsAsync(5, 10, UserRoles.Instructor);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task AuthorizeOrderMaterials_NotOnBomNorStage_Fails()
    {
        var sut = CreateSut();
        _orders.Setup(o => o.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 5, ProductName = "Camisa" });
        _flowRepo.Setup(f => f.GetStagesByOrderAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage { Id = 1, ProductionOrderId = 5, InstructorUserId = 99, Name = "Trazo" }
            ]);
        _boms.Setup(b => b.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BomProduct
            {
                ProductName = "Camisa",
                Instructors = [new BomProductInstructor { UserId = 77, BomProductId = 1 }]
            });

        var result = await sut.AuthorizeOrderMaterialsAsync(5, 10, UserRoles.Instructor);

        Assert.False(result.Success);
        Assert.Contains("No está habilitado", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizeOrderMaterials_EmptyBomAndNoStageAssignee_FailsExplicit()
    {
        var sut = CreateSut();
        _orders.Setup(o => o.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 5, ProductName = "Camisa" });
        _flowRepo.Setup(f => f.GetStagesByOrderAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage { Id = 1, ProductionOrderId = 5, InstructorUserId = null, Name = "Trazo" }
            ]);
        _boms.Setup(b => b.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BomProduct { ProductName = "Camisa", Instructors = [] });

        var result = await sut.AuthorizeOrderMaterialsAsync(5, 10, UserRoles.Instructor);

        Assert.False(result.Success);
        Assert.Contains("no tiene instructores habilitados", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no tiene etapa asignada", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizeOrderMaterials_Administrador_AlwaysSucceeds()
    {
        var sut = CreateSut();
        var result = await sut.AuthorizeOrderMaterialsAsync(5, 1, UserRoles.Administrador);
        Assert.True(result.Success);
        _orders.Verify(o => o.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersAsync_IncludesBomEligible_WithoutOpeningProductionFlag()
    {
        var sut = CreateSut();
        var order = new ProductionOrder
        {
            Id = 5,
            OrderNumber = "OP-105",
            ProductName = "Camisa",
            TotalQuantity = 10,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso,
            Deadline = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        };
        _orders.Setup(o => o.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([order]);
        _flowRepo.Setup(f => f.GetStagesByOrderAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage { Id = 1, ProductionOrderId = 5, InstructorUserId = 99, Name = "Trazo" }
            ]);
        _boms.Setup(b => b.GetProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BomProduct
                {
                    ProductName = "Camisa",
                    Instructors = [new BomProductInstructor { UserId = 10, BomProductId = 1 }]
                }
            ]);
        _snapshots.Setup(s => s.GetByOrderIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _boms.Setup(b => b.GetByProductAsync("Camisa", It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var list = await sut.GetOrdersAsync(10, UserRoles.Instructor, "Laura");

        Assert.Single(list);
        Assert.True(list[0].CanManageMaterials);
        Assert.False(list[0].CanOperateProduction);
    }

    [Fact]
    public async Task CanAccessOrderAsync_BomInstructorOnly_StillDeniedForMes()
    {
        var sut = CreateSut();
        var order = new ProductionOrder { Id = 5, ProductName = "Camisa" };
        _orders.Setup(o => o.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([order]);
        _orders.Setup(o => o.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _flowRepo.Setup(f => f.GetStagesByOrderAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderStage { Id = 1, ProductionOrderId = 5, InstructorUserId = 99, Name = "Trazo" }
            ]);
        _boms.Setup(b => b.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BomProduct
            {
                ProductName = "Camisa",
                Instructors = [new BomProductInstructor { UserId = 10, BomProductId = 1 }]
            });

        var canAccess = await sut.CanAccessOrderAsync(5, 10, UserRoles.Instructor, "Laura");
        var materials = await sut.AuthorizeOrderMaterialsAsync(5, 10, UserRoles.Instructor);

        Assert.False(canAccess);
        Assert.True(materials.Success);
    }
}
