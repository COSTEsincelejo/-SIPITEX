using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class MaterialConsumptionServiceTests
{
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IOrderMaterialRequirementRepository> _requirements = new();
    private readonly Mock<IStockMovementRepository> _stock = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private MaterialConsumptionService CreateSut()
    {
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        return new(
            _consumos.Object,
            _orders.Object,
            _materials.Object,
            _users.Object,
            _requirements.Object,
            new MaterialConsumptionCostService(),
            _uow.Object);
    }

    [Fact]
    public async Task EntregarDiezYConsumirDiez_DejaElStockEn490()
    {
        var material = new Material
        {
            Id = 5,
            Name = "Tela",
            Stock = 500,
            CostoPromedioPonderado = 12,
            Unit = MaterialUnit.Metros
        };
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-1",
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.PendienteRevisionPlantaInventario
        };
        var line = new ProductionOrderMaterialRequirement
        {
            Id = 9,
            ProductionOrderId = 1,
            MaterialId = 5,
            Material = material,
            QuantityRequired = 10,
            Unit = MaterialUnit.Metros
        };

        var orders = new Mock<IProductionOrderRepository>();
        var reqs = new Mock<IOrderMaterialRequirementRepository>();
        var materials = new Mock<IMaterialRepository>();
        var movements = new Mock<IStockMovementRepository>();
        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        reqs.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);

        var entrega = new OrderMaterialService(
            orders.Object, reqs.Object, materials.Object, snapshots.Object, movements.Object, uow.Object);
        var entregado = await entrega.DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 10)], null), 3);

        Assert.True(entregado.Success, entregado.Message);
        Assert.Equal(490m, material.Stock);
        Assert.Equal(10m, line.QuantityDelivered);

        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _materials.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _requirements.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);

        var consumido = await CreateSut().RegisterAsync(new RegisterConsumoMaterialDto(1, 5, 10, 7));

        Assert.True(consumido.Success, consumido.Message);
        Assert.Equal(490m, material.Stock);
        Assert.Equal(10m, line.QuantityConsumed);
        _stock.Verify(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Never);
        movements.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<StockMovement>>(m => m.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_MaterialAjenoALaOrden_Falla()
    {
        var material = new Material { Id = 2, Name = "Botón", Stock = 40 };
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-9", Status = OrderStatus.EnProceso });
        _materials.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _requirements.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderMaterialRequirement { MaterialId = 8, QuantityDelivered = 5 }
            ]);

        var result = await CreateSut().RegisterAsync(new RegisterConsumoMaterialDto(1, 2, 1, 7));

        Assert.False(result.Success);
        Assert.Contains("no pertenece a la orden", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(40m, material.Stock);
        _uow.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_MasDeLoEntregadoMenosLoConsumido_Falla()
    {
        PrepararLinea(entregado: 10, consumido: 4, costoAcumulado: 80);

        var result = await CreateSut().RegisterAsync(new RegisterConsumoMaterialDto(1, 2, 7, 7));

        Assert.False(result.Success);
        Assert.Contains("Solo se entregaron 10 de «Tela»", result.Message, StringComparison.Ordinal);
        Assert.Contains("ya consumiste 4", result.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OrderStatus.Finalizada, "finalizada")]
    [InlineData(OrderStatus.Cancelada, "cancelada")]
    public async Task RegisterAsync_OrdenCerrada_Falla(OrderStatus estado, string esperado)
    {
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-1", Status = estado });

        var result = await CreateSut().RegisterAsync(new RegisterConsumoMaterialDto(1, 2, 1, 7));

        Assert.False(result.Success);
        Assert.Contains(esperado, result.Message, StringComparison.OrdinalIgnoreCase);
        _materials.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_UsaElCostoDeLaEntrega_AunqueCambieElPromedioDelMaterial()
    {
        var material = PrepararLinea(entregado: 10, consumido: 0, costoAcumulado: 80);
        material.CostoPromedioPonderado = 40m;

        ConsumoMaterial? saved = null;
        _consumos.Setup(r => r.AddAsync(It.IsAny<ConsumoMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<ConsumoMaterial, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().RegisterAsync(new RegisterConsumoMaterialDto(1, 2, 4, 7));

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal(8m, saved!.CostoUnitarioAlMomento);
        Assert.Equal(20m, material.Stock);
        material.CostoPromedioPonderado = 99m;
        Assert.Equal(8m, saved.CostoUnitarioAlMomento);
    }

    [Fact]
    public async Task GetCostoPromedioByOrderAsync_UsesWeightedAverage()
    {
        _consumos.Setup(r => r.GetByOrderIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial { Cantidad = 2, CostoUnitarioAlMomento = 10 },
                new ConsumoMaterial { Cantidad = 6, CostoUnitarioAlMomento = 20 }
            ]);

        var resumen = await CreateSut().GetCostoPromedioByOrderAsync(4);

        Assert.Equal(8, resumen.CantidadTotal);
        Assert.Equal(17.5m, resumen.CostoPromedioPonderado);
        Assert.Equal(140, resumen.CostoTotal);
    }

    private Material PrepararLinea(decimal entregado, decimal consumido, decimal costoAcumulado)
    {
        var material = new Material
        {
            Id = 2,
            Name = "Tela",
            Stock = 20,
            CostoPromedioPonderado = 15m,
            Unit = MaterialUnit.Metros
        };
        var line = new ProductionOrderMaterialRequirement
        {
            ProductionOrderId = 1,
            MaterialId = 2,
            Material = material,
            QuantityDelivered = entregado,
            QuantityConsumed = consumido,
            CostoEntregadoAcumulado = costoAcumulado
        };
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-9", Status = OrderStatus.EnProceso });
        _materials.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _requirements.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);
        return material;
    }
}
