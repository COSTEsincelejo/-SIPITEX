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
/// Camino NoAplica: descuento BOM + StockMovement. Anti-doble frente a entrega+consumo.
/// </summary>
public class ProductionConsumptionLedgerTests
{
    [Fact]
    public async Task NoAplica_RegisterProduction_DescuentaStockYCreaUnStockMovementPorMaterial()
    {
        var materialA = new Material
        {
            Id = 1,
            Name = "Tela",
            Stock = 100,
            CostoPromedioPonderado = 5,
            Unit = MaterialUnit.Metros
        };
        var materialB = new Material
        {
            Id = 2,
            Name = "Hilo",
            Stock = 50,
            CostoPromedioPonderado = 1,
            Unit = MaterialUnit.Metros
        };
        var order = new ProductionOrder
        {
            Id = 11,
            OrderNumber = "OP-211",
            ProductName = "Camisa",
            TotalQuantity = 20,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.NoAplica
        };
        var movements = new List<StockMovement>();

        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        snapshots.Setup(s => s.GetByOrderIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderBomSnapshot
                {
                    ProductionOrderId = 11,
                    MaterialId = 1,
                    MaterialName = "Tela",
                    QuantityPerUnit = 2m,
                    Unit = MaterialUnit.Metros
                },
                new ProductionOrderBomSnapshot
                {
                    ProductionOrderId = 11,
                    MaterialId = 2,
                    MaterialName = "Hilo",
                    QuantityPerUnit = 0.5m,
                    Unit = MaterialUnit.Metros
                }
            ]);

        var materials = new Mock<IMaterialRepository>();
        materials.Setup(m => m.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(materialA);
        materials.Setup(m => m.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(materialB);

        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<StockMovement>, CancellationToken>((rows, _) => movements.AddRange(rows))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var flowService = new Mock<IProductionFlowService>();
        flowService.Setup(s => s.LogProductionRegisteredAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var requirements = new Mock<IOrderMaterialRequirementRepository>();
        requirements.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new ProductionOrderService(
            orders.Object,
            new Mock<IBomRepository>().Object,
            snapshots.Object,
            requirements.Object,
            new Mock<IProductionFlowRepository>().Object,
            flowService.Object,
            new Mock<IOrderChangeLogRepository>().Object,
            new Mock<IFichaRepository>().Object,
            uow.Object,
            new ProductionConsumptionService(
                new Mock<IBomRepository>().Object,
                materials.Object,
                stock.Object),
            NullLogger<ProductionOrderService>.Instance);

        var result = await sut.RegisterProductionAsync(11, 4, actorUserId: 9);

        Assert.True(result.Success, result.Message);
        Assert.Equal(92m, materialA.Stock); // 100 - 2*4
        Assert.Equal(48m, materialB.Stock); // 50 - 0.5*4
        Assert.Equal(4, order.ProducedQuantity);
        Assert.Equal(2, movements.Count);
        Assert.All(movements, m =>
        {
            Assert.Equal(StockMovementType.Salida, m.TipoMovimiento);
            Assert.Equal("Orden:11", m.Referencia);
            Assert.Equal(9, m.UsuarioId);
        });
        Assert.Contains(movements, m => m.MaterialId == 1 && m.Cantidad == 8m && m.StockResultante == 92m);
        Assert.Contains(movements, m => m.MaterialId == 2 && m.Cantidad == 2m && m.StockResultante == 48m);
    }

    [Fact]
    public async Task ListaParaProduccion_RegisterProduction_NoDescuentaNiCreaMovimiento()
    {
        var material = new Material { Id = 1, Name = "Tela", Stock = 30, Unit = MaterialUnit.Metros };
        var order = new ProductionOrder
        {
            Id = 12,
            OrderNumber = "OP-212",
            ProductName = "Camisa",
            TotalQuantity = 10,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.ListaParaProduccion
        };
        var movements = new List<StockMovement>();

        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var materials = new Mock<IMaterialRepository>();
        materials.Setup(m => m.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(material);

        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<StockMovement>, CancellationToken>((rows, _) => movements.AddRange(rows))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var flowService = new Mock<IProductionFlowService>();
        flowService.Setup(s => s.LogProductionRegisteredAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        snapshots.Setup(s => s.GetByOrderIdAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderBomSnapshot
                {
                    ProductionOrderId = 12,
                    MaterialId = 1,
                    QuantityPerUnit = 1m,
                    Unit = MaterialUnit.Metros
                }
            ]);

        var sut = new ProductionOrderService(
            orders.Object,
            new Mock<IBomRepository>().Object,
            snapshots.Object,
            new Mock<IOrderMaterialRequirementRepository>().Object,
            new Mock<IProductionFlowRepository>().Object,
            flowService.Object,
            new Mock<IOrderChangeLogRepository>().Object,
            new Mock<IFichaRepository>().Object,
            uow.Object,
            new ProductionConsumptionService(
                new Mock<IBomRepository>().Object,
                materials.Object,
                stock.Object),
            NullLogger<ProductionOrderService>.Instance);

        var result = await sut.RegisterProductionAsync(12, 3, actorUserId: 9);

        Assert.True(result.Success, result.Message);
        Assert.Equal(30m, material.Stock);
        Assert.Empty(movements);
        Assert.Equal(3, order.ProducedQuantity);
        stock.Verify(
            s => s.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EntregaLuegoConsumo_Y_NoAplicaSeparado_NoDobleDescuentoEnMismaOrdenConBodega()
    {
        // Flujo bodega: Deliver baja stock + Salida; RegisterProduction con ListaParaProduccion no toca stock.
        var material = new Material
        {
            Id = 5,
            Name = "Tela",
            Stock = 20,
            CostoPromedioPonderado = 8,
            Unit = MaterialUnit.Metros
        };
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-301",
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.MaterialesValidados,
            TotalQuantity = 10,
            ProducedQuantity = 0,
            ProductName = "Camisa"
        };
        var line = new ProductionOrderMaterialRequirement
        {
            Id = 9,
            ProductionOrderId = 1,
            MaterialId = 5,
            Material = material,
            QuantityRequired = 8,
            QuantityDelivered = 0,
            Unit = MaterialUnit.Metros
        };
        var movements = new List<StockMovement>();

        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var reqs = new Mock<IOrderMaterialRequirementRepository>();
        reqs.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);

        var materials = new Mock<IMaterialRepository>();
        materials.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(material);

        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<StockMovement>, CancellationToken>((rows, _) => movements.AddRange(rows))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var entrega = new OrderMaterialService(
            orders.Object,
            reqs.Object,
            materials.Object,
            new Mock<IProductionOrderBomSnapshotRepository>().Object,
            stock.Object,
            uow.Object);

        var entregado = await entrega.DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 8)], null),
            encargadoDeBodegaId: 3);
        Assert.True(entregado.Success, entregado.Message);
        Assert.Equal(12m, material.Stock);
        Assert.Equal(OrderMaterialsStatus.ListaParaProduccion, order.MaterialsStatus);
        Assert.Single(movements);

        var flowService = new Mock<IProductionFlowService>();
        flowService.Setup(s => s.LogProductionRegisteredAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var production = new ProductionOrderService(
            orders.Object,
            new Mock<IBomRepository>().Object,
            new Mock<IProductionOrderBomSnapshotRepository>().Object,
            reqs.Object,
            new Mock<IProductionFlowRepository>().Object,
            flowService.Object,
            new Mock<IOrderChangeLogRepository>().Object,
            new Mock<IFichaRepository>().Object,
            uow.Object,
            new ProductionConsumptionService(
                new Mock<IBomRepository>().Object,
                materials.Object,
                stock.Object),
            NullLogger<ProductionOrderService>.Instance);

        var prod = await production.RegisterProductionAsync(1, 2, actorUserId: 3);
        Assert.True(prod.Success, prod.Message);
        Assert.Equal(12m, material.Stock);
        Assert.Single(movements);
        Assert.Equal(2, order.ProducedQuantity);
    }
}
