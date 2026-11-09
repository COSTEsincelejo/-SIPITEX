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
    public async Task RegisterAsync_SinEntrega_NoTocaStock()
    {
        var material = new Material { Id = 2, Name = "Tela", Stock = 20, CostoAdquisicion = 10 };
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-1" });
        _materials.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _requirements.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _consumos.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateSut().RegisterAsync(new RegisterConsumoMaterialDto(1, 2, 5, 7));

        Assert.False(result.Success);
        Assert.Equal(MaterialConsumptionService.MensajeSinEntrega, result.Message);
        Assert.Equal(20, material.Stock);
        _uow.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ConEntrega_NoDescuentaStockYCongelaCosto()
    {
        var material = new Material
        {
            Id = 2,
            Name = "Tela",
            Stock = 16,
            CostoAdquisicion = 12.5m,
            CostoPromedioPonderado = 12.5m,
            Unit = MaterialUnit.Metros
        };
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-9" });
        _materials.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _requirements.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderMaterialRequirement
                {
                    Id = 4,
                    ProductionOrderId = 1,
                    MaterialId = 2,
                    QuantityDelivered = 4
                }
            ]);
        _consumos.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        ConsumoMaterial? saved = null;
        _consumos.Setup(r => r.AddAsync(It.IsAny<ConsumoMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<ConsumoMaterial, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().RegisterAsync(new RegisterConsumoMaterialDto(1, 2, 4, 7));

        Assert.True(result.Success, result.Message);
        Assert.Equal(16, material.Stock);
        Assert.NotNull(saved);
        Assert.Equal(4, saved!.OrderMaterialRequirementId);
        Assert.Equal(12.5m, saved.CostoUnitarioAlMomento);
        Assert.Equal(4, saved.Cantidad);
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

    [Fact]
    public async Task RegisterAsync_SnapshotNoSigueAlPromedioPosterior()
    {
        var material = new Material
        {
            Id = 2,
            Name = "Tela",
            Stock = 20,
            CostoPromedioPonderado = 15m,
            CostoAdquisicion = 15m
        };
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-1" });
        _materials.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _requirements.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderMaterialRequirement
                {
                    Id = 4,
                    ProductionOrderId = 1,
                    MaterialId = 2,
                    QuantityDelivered = 3
                }
            ]);
        _consumos.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        ConsumoMaterial? saved = null;
        _consumos.Setup(r => r.AddAsync(It.IsAny<ConsumoMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<ConsumoMaterial, CancellationToken>((c, _) => saved = c)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().RegisterAsync(new RegisterConsumoMaterialDto(1, 2, 3, 7));
        Assert.True(result.Success, result.Message);
        Assert.Equal(20, material.Stock);
        Assert.Equal(15m, saved!.CostoUnitarioAlMomento);

        material.CostoPromedioPonderado = 40m;
        material.CostoAdquisicion = 40m;

        Assert.Equal(15m, saved.CostoUnitarioAlMomento);
    }
}
