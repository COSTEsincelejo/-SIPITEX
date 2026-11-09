using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class StockUnicoDescuentoTests
{
    [Fact]
    public async Task EntregaYConsumoDeLaMismaCantidad_DescuentanStockUnaSolaVez()
    {
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
            OrderNumber = "OP-101",
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.MaterialesValidados
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
        var consumos = new List<ConsumoMaterial>();
        var movements = new List<StockMovement>();

        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var reqs = new Mock<IOrderMaterialRequirementRepository>();
        reqs.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);

        var materials = new Mock<IMaterialRepository>();
        materials.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(material);

        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });

        var consumosRepo = new Mock<IConsumoMaterialRepository>();
        consumosRepo.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<IReadOnlyList<ConsumoMaterial>>(consumos.ToList()));
        consumosRepo.Setup(r => r.AddAsync(It.IsAny<ConsumoMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<ConsumoMaterial, CancellationToken>((c, _) => consumos.Add(c))
            .Returns(Task.CompletedTask);

        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<StockMovement>, CancellationToken>((rows, _) => movements.AddRange(rows))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var entrega = new OrderMaterialService(
            orders.Object,
            reqs.Object,
            materials.Object,
            new Mock<IProductionOrderBomSnapshotRepository>().Object,
            stock.Object,
            uow.Object);
        var consumo = new MaterialConsumptionService(
            consumosRepo.Object,
            orders.Object,
            materials.Object,
            users.Object,
            reqs.Object,
            new MaterialConsumptionCostService(),
            uow.Object);

        var entregado = await entrega.DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 8)], null),
            encargadoDeBodegaId: 3);

        Assert.True(entregado.Success, entregado.Message);
        Assert.Equal(12m, material.Stock);
        Assert.Equal(8m, line.QuantityDelivered);
        var salida = Assert.Single(movements);
        Assert.Equal(StockMovementType.Salida, salida.TipoMovimiento);
        Assert.Equal(8m, salida.Cantidad);
        Assert.Equal(12m, salida.StockResultante);
        Assert.Equal("Orden:1", salida.Referencia);

        var usado = await consumo.RegisterAsync(new RegisterConsumoMaterialDto(1, 5, 8, 7));

        Assert.True(usado.Success, usado.Message);
        Assert.Equal(12m, material.Stock);
        Assert.Single(movements);
        var ficha = Assert.Single(consumos);
        Assert.Equal(9, ficha.OrderMaterialRequirementId);
        Assert.Equal(8m, ficha.Cantidad);

        var deMas = await consumo.RegisterAsync(new RegisterConsumoMaterialDto(1, 5, 1, 7));

        Assert.False(deMas.Success);
        Assert.Contains(MaterialConsumptionService.MensajeSuperaEntrega, deMas.Message);
        Assert.Equal(12m, material.Stock);
        Assert.Single(movements);
        Assert.Single(consumos);
    }

    [Fact]
    public async Task ConsumirSinEntrega_FallaYNoCreaSalida()
    {
        var material = new Material { Id = 5, Name = "Hilo", Stock = 10, Unit = MaterialUnit.Metros };
        var order = new ProductionOrder { Id = 2, OrderNumber = "OP-102", Status = OrderStatus.EnProceso };
        var movements = new List<StockMovement>();

        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var reqs = new Mock<IOrderMaterialRequirementRepository>();
        reqs.Setup(r => r.GetByOrderIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var materials = new Mock<IMaterialRepository>();
        materials.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        var consumosRepo = new Mock<IConsumoMaterialRepository>();
        consumosRepo.Setup(r => r.GetByOrderIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var stock = new Mock<IStockMovementRepository>();
        stock.Setup(s => s.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((row, _) => movements.Add(row))
            .Returns(Task.CompletedTask);
        var uow = new Mock<IUnitOfWork>();

        var consumo = new MaterialConsumptionService(
            consumosRepo.Object,
            orders.Object,
            materials.Object,
            users.Object,
            reqs.Object,
            new MaterialConsumptionCostService(),
            uow.Object);

        var result = await consumo.RegisterAsync(new RegisterConsumoMaterialDto(2, 5, 4, 7));

        Assert.False(result.Success);
        Assert.Equal(MaterialConsumptionService.MensajeSinEntrega, result.Message);
        Assert.Equal(10m, material.Stock);
        Assert.Empty(movements);
    }
}
