using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
/// Gap #2: editar/cancelar órdenes. Aprobar = implícito al Create (EnProceso).
/// </summary>
public class ProductionOrderEditCancelTests
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

    private ProductionOrderService CreateSut()
    {
        _requirements.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _flowRepo.Setup(r => r.GetStagesByOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return new(_orders.Object, _boms.Object, _snapshots.Object, _requirements.Object,
            _flowRepo.Object, _flowService.Object, _changeLogs.Object, _fichas.Object, _uow.Object,
            new ProductionConsumptionService(_boms.Object, _materials.Object));
    }

    private static BomProduct Product(string name = "Camisa") => new()
    {
        Id = 1,
        ProductName = name,
        HabilitadoParaOrdenes = true,
        Items =
        [
            new BomItem
            {
                MaterialId = 1,
                Material = new Material { Id = 1, Code = "mat1", Name = "Tela", Unit = MaterialUnit.Metros, Stock = 200 },
                QuantityPerUnit = 1.5m,
                Unit = MaterialUnit.Metros,
                ProductName = name
            }
        ]
    };

    [Fact]
    public async Task UpdateOrderAsync_EachChangedField_WritesOrderChangeLog()
    {
        var order = new ProductionOrder
        {
            Id = 7,
            OrderNumber = "OP-107",
            ProductName = "Camisa",
            ClientName = "Cliente A",
            TotalQuantity = 50,
            ProducedQuantity = 10,
            Deadline = new DateOnly(2026, 9, 1),
            Status = OrderStatus.EnProceso
        };
        _orders.Setup(o => o.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _boms.Setup(b => b.GetProductByNameAsync("Pantalón", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Product("Pantalón"));
        _snapshots.Setup(s => s.GetByOrderIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        List<OrderChangeLog>? captured = null;
        _changeLogs
            .Setup(c => c.AddRangeAsync(It.IsAny<IEnumerable<OrderChangeLog>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<OrderChangeLog>, CancellationToken>((rows, _) => captured = rows.ToList())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().UpdateOrderAsync(
            new UpdateProductionOrderDto(7, "Pantalón", 80, new DateOnly(2026, 10, 15), "Cliente B"),
            actorUserId: 1);

        Assert.True(result.Success);
        Assert.Equal("Pantalón", order.ProductName);
        Assert.Equal(80, order.TotalQuantity);
        Assert.Equal(new DateOnly(2026, 10, 15), order.Deadline);
        Assert.Equal("Cliente B", order.ClientName);
        Assert.NotNull(captured);
        Assert.Equal(4, captured!.Count);
        Assert.Contains(captured, c => c.Campo == nameof(ProductionOrder.ProductName)
            && c.ValorAnterior == "Camisa" && c.ValorNuevo == "Pantalón" && c.UsuarioId == 1);
        Assert.Contains(captured, c => c.Campo == nameof(ProductionOrder.TotalQuantity)
            && c.ValorAnterior == "50" && c.ValorNuevo == "80");
        Assert.Contains(captured, c => c.Campo == nameof(ProductionOrder.Deadline)
            && c.ValorAnterior == "2026-09-01" && c.ValorNuevo == "2026-10-15");
        Assert.Contains(captured, c => c.Campo == nameof(ProductionOrder.ClientName)
            && c.ValorAnterior == "Cliente A" && c.ValorNuevo == "Cliente B");
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateOrderAsync_CancelledOrder_Fails()
    {
        var order = new ProductionOrder { Id = 3, Status = OrderStatus.Cancelada, ProductName = "Camisa", TotalQuantity = 10 };
        _orders.Setup(o => o.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateSut().UpdateOrderAsync(
            new UpdateProductionOrderDto(3, "Camisa", 12, DateOnly.FromDateTime(DateTime.Today), null),
            1);

        Assert.False(result.Success);
        Assert.Contains("cancelada", result.Message, StringComparison.OrdinalIgnoreCase);
        _changeLogs.Verify(
            c => c.AddRangeAsync(It.IsAny<IEnumerable<OrderChangeLog>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_SetsCancelada_WithoutStockMovement()
    {
        var order = new ProductionOrder
        {
            Id = 9,
            OrderNumber = "OP-109",
            Status = OrderStatus.EnProceso,
            ProductName = "Camisa",
            TotalQuantity = 20
        };
        _orders.Setup(o => o.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        List<OrderChangeLog>? captured = null;
        _changeLogs
            .Setup(c => c.AddRangeAsync(It.IsAny<IEnumerable<OrderChangeLog>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<OrderChangeLog>, CancellationToken>((rows, _) => captured = rows.ToList())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CancelOrderAsync(9, actorUserId: 2);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Cancelada, order.Status);
        Assert.NotNull(captured);
        Assert.Single(captured!);
        Assert.Equal(nameof(ProductionOrder.Status), captured![0].Campo);
        Assert.Equal(nameof(OrderStatus.EnProceso), captured[0].ValorAnterior);
        Assert.Equal(nameof(OrderStatus.Cancelada), captured[0].ValorNuevo);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _stockMovements.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterProductionAsync_OnCancelledOrder_Fails()
    {
        var order = new ProductionOrder
        {
            Id = 4,
            Status = OrderStatus.Cancelada,
            TotalQuantity = 100,
            ProducedQuantity = 0,
            MaterialsStatus = OrderMaterialsStatus.NoAplica
        };
        _orders.Setup(o => o.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await CreateSut().RegisterProductionAsync(4, 5);

        Assert.False(result.Success);
        Assert.Contains("cancelada", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, order.ProducedQuantity);
    }

    [Fact]
    public async Task DeliverAsync_OnCancelledOrder_Fails()
    {
        var material = new Material { Id = 5, Name = "Hilo", Stock = 50, Unit = MaterialUnit.Metros };
        var order = new ProductionOrder
        {
            Id = 1,
            Status = OrderStatus.Cancelada,
            MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega
        };
        var line = new ProductionOrderMaterialRequirement
        {
            Id = 9, ProductionOrderId = 1, MaterialId = 5, Material = material,
            QuantityRequired = 10, QuantityDelivered = 0, Unit = MaterialUnit.Metros
        };

        var orders = new Mock<IProductionOrderRepository>();
        var reqs = new Mock<IOrderMaterialRequirementRepository>();
        var materials = new Mock<IMaterialRepository>();
        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        var stock = new Mock<IStockMovementRepository>();
        var uow = new Mock<IUnitOfWork>();
        orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        reqs.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);

        var sut = new OrderMaterialService(
            orders.Object, reqs.Object, materials.Object, snapshots.Object, stock.Object, uow.Object);

        var result = await sut.DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 5)], null),
            bodegueroId: 3);

        Assert.False(result.Success);
        Assert.Equal(50m, material.Stock);
        stock.Verify(s => s.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void OrdenesController_EditAndCancel_AreAdministradorOnly()
    {
        foreach (var methodName in new[] { nameof(OrdenesController.Edit), nameof(OrdenesController.Cancel) })
        {
            var methods = typeof(OrdenesController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == methodName);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<AuthorizeAttribute>();
                Assert.NotNull(attr);
                Assert.Equal(UserRoles.Administrador, attr!.Roles);
                Assert.DoesNotContain(UserRoles.Instructor, attr.Roles!, StringComparison.Ordinal);
                Assert.DoesNotContain(UserRoles.Bodeguero, attr.Roles!, StringComparison.Ordinal);
            }
        }
    }
}
