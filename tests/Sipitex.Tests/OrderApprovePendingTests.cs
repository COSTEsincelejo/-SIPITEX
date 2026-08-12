using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

/// <summary>
/// Flujo Pendiente → Aprobar (Admin): Create nace Pendiente; producción/MES/Deliver bloqueados hasta EnProceso.
/// </summary>
public class OrderApprovePendingTests
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
    private readonly Mock<IStockMovementRepository> _stockMovements = new();

    private ProductionOrderService CreateOrderSut()
    {
        _requirements.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _flowService.Setup(s => s.EnsureStagesForOrderAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return new(_orders.Object, _boms.Object, _snapshots.Object, _requirements.Object,
            _flowRepo.Object, _flowService.Object, _changeLogs.Object, _fichas.Object, _uow.Object,
            new ProductionConsumptionService(_boms.Object, _materials.Object));
    }

    private static BomProduct EnabledCamisa() => new()
    {
        ProductName = "Camisa",
        HabilitadoParaOrdenes = true,
        Items =
        [
            new BomItem
            {
                ProductName = "Camisa",
                MaterialId = 1,
                Material = new Material { Id = 1, Code = "mat1", Name = "Tela", Unit = MaterialUnit.Metros },
                QuantityPerUnit = 1.6m,
                Unit = MaterialUnit.Metros
            }
        ]
    };

    [Fact]
    public async Task CreateOrderAsync_StartsInPendiente()
    {
        _boms.Setup(r => r.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EnabledCamisa());
        _orders.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        ProductionOrder? saved = null;
        _orders.Setup(r => r.AddAsync(It.IsAny<ProductionOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ProductionOrder, CancellationToken>((o, _) => { o.Id = 1; saved = o; })
            .Returns(Task.CompletedTask);

        var result = await CreateOrderSut().CreateOrderAsync(
            new CreateProductionOrderDto("Camisa", 10, DateOnly.FromDateTime(DateTime.Today.AddDays(5))));

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal(OrderStatus.Pendiente, saved!.Status);
        _flowService.Verify(s => s.EnsureStagesForOrderAsync(1, "Sistema", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveOrderAsync_AdminTransitionsPendienteToEnProceso()
    {
        var order = new ProductionOrder
        {
            Id = 4,
            OrderNumber = "OP-104",
            ProductName = "Camisa",
            Status = OrderStatus.Pendiente,
            TotalQuantity = 10
        };
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        List<OrderChangeLog>? logs = null;
        _changeLogs.Setup(c => c.AddRangeAsync(It.IsAny<IEnumerable<OrderChangeLog>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<OrderChangeLog>, CancellationToken>((rows, _) => logs = rows.ToList())
            .Returns(Task.CompletedTask);

        var result = await CreateOrderSut().ApproveOrderAsync(4, actorUserId: 1);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.EnProceso, order.Status);
        Assert.NotNull(logs);
        Assert.Single(logs!);
        Assert.Equal(nameof(ProductionOrder.Status), logs![0].Campo);
        Assert.Equal(nameof(OrderStatus.Pendiente), logs[0].ValorAnterior);
        Assert.Equal(nameof(OrderStatus.EnProceso), logs[0].ValorNuevo);
    }

    [Fact]
    public async Task ApproveOrderAsync_WhenAlreadyEnProceso_Fails()
    {
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 4, Status = OrderStatus.EnProceso, OrderNumber = "OP-104" });

        var result = await CreateOrderSut().ApproveOrderAsync(4, 1);

        Assert.False(result.Success);
        Assert.Contains("ya está aprobada", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrdenesController_Approve_IsAdministradorOnly()
    {
        var method = typeof(OrdenesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nameof(OrdenesController.Approve));
        var attr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(UserRoles.Administrador, attr!.Roles);
    }

    [Fact]
    public async Task RegisterProductionAsync_BlockedWhilePendiente()
    {
        _orders.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder
            {
                Id = 2,
                OrderNumber = "OP-102",
                Status = OrderStatus.Pendiente,
                TotalQuantity = 10,
                ProducedQuantity = 0,
                MaterialsStatus = OrderMaterialsStatus.NoAplica
            });

        var result = await CreateOrderSut().RegisterProductionAsync(2, 1);

        Assert.False(result.Success);
        Assert.Contains("pendiente de aprobación", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeliverAsync_BlockedWhilePendiente()
    {
        var material = new Material { Id = 5, Name = "Hilo", Unit = MaterialUnit.Metros, Stock = 50 };
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-101",
            Status = OrderStatus.Pendiente,
            MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega
        };
        var line = new ProductionOrderMaterialRequirement
        {
            Id = 9,
            ProductionOrderId = 1,
            MaterialId = 5,
            Material = material,
            QuantityRequired = 10,
            QuantityDelivered = 0,
            Unit = MaterialUnit.Metros
        };
        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _requirements.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);

        var sut = new OrderMaterialService(
            _orders.Object, _requirements.Object, _materials.Object, _snapshots.Object,
            _stockMovements.Object, _uow.Object);

        var result = await sut.DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 5)], null),
            bodegueroId: 3);

        Assert.False(result.Success);
        Assert.Contains("pendiente de aprobación", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(50m, material.Stock);
    }

    [Fact]
    public async Task AddMaterialAsync_AllowedWhilePendiente()
    {
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-101",
            Status = OrderStatus.Pendiente,
            MaterialsStatus = OrderMaterialsStatus.NoAplica
        };
        var material = new Material { Id = 5, Name = "Hilo", Unit = MaterialUnit.Metros, Stock = 10 };
        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _materials.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _requirements.Setup(r => r.ExistsAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _requirements.Setup(r => r.AddAsync(It.IsAny<ProductionOrderMaterialRequirement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new OrderMaterialService(
            _orders.Object, _requirements.Object, _materials.Object, _snapshots.Object,
            _stockMovements.Object, _uow.Object);

        var result = await sut.AddMaterialAsync(new AddOrderMaterialDto(1, 5, 2m, null));

        Assert.True(result.Success);
        Assert.Equal(OrderMaterialsStatus.PendienteRevisionBodega, order.MaterialsStatus);
    }

    [Fact]
    public async Task SendToNext_BlockedWhilePendiente()
    {
        var from = new ProductionOrderStage
        {
            Id = 10, ProductionOrderId = 1, Name = "Trazo", SortOrder = 1,
            QuantityReceived = 50, Status = ProductionStageStatus.EnProceso
        };
        var order = new ProductionOrder
        {
            Id = 1, OrderNumber = "OP-001", ProductName = "Camisa", Status = OrderStatus.Pendiente
        };

        var flow = new Mock<IProductionFlowRepository>();
        var materialsReq = new Mock<IOrderMaterialRequirementRepository>();
        var users = new Mock<IUserRepository>();
        flow.Setup(f => f.GetStageByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(from);
        flow.Setup(f => f.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([from]);
        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var sut = new ProductionFlowService(
            _orders.Object, flow.Object, materialsReq.Object, _snapshots.Object, _boms.Object, users.Object,
            _materials.Object, _stockMovements.Object, _uow.Object);

        var result = await sut.SendToNextAsync(
            new SendToNextStageDto(10, 5, null),
            actorUserId: 1, actorName: "Admin", actorRole: UserRoles.Administrador);

        Assert.False(result.Success);
        Assert.Contains("pendiente de aprobación", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDashboardAsync_SeparatesPendienteFromActiveEnProceso()
    {
        var orders = new Mock<IProductionOrderService>();
        var materials = new Mock<IMaterialRepository>();
        var quality = new Mock<IQualityRepository>();
        orders.Setup(s => s.GetOrdersAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderDto(1, "OP-1", "A", 10, 0, 0, OrderStatus.Pendiente, DateOnly.FromDateTime(DateTime.Today), ""),
                new ProductionOrderDto(2, "OP-2", "B", 10, 3, 30, OrderStatus.EnProceso, DateOnly.FromDateTime(DateTime.Today), ""),
                new ProductionOrderDto(3, "OP-3", "C", 10, 10, 100, OrderStatus.Finalizada, DateOnly.FromDateTime(DateTime.Today), "")
            ]);
        materials.Setup(m => m.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        quality.Setup(q => q.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var dash = await new StatisticsService(orders.Object, materials.Object, quality.Object)
            .GetDashboardAsync();

        Assert.Equal(1, dash.ActiveOrders);
        Assert.Equal(1, dash.PendingApprovalOrders);
        Assert.Equal(13, dash.TotalProduced);
    }
}
