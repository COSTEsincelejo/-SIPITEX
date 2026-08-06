using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class ProductionOrderServiceTests
{
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snapshots = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMaterialRepository> _materials = new();

    private ProductionOrderService CreateSut() =>
        new(_orders.Object, _boms.Object, _snapshots.Object, _uow.Object,
            new ProductionConsumptionService(_boms.Object, _materials.Object));

    [Fact]
    public async Task CreateOrderAsync_WhenProductOrQuantityMissing_FailsWithObligatoryMessage()
    {
        var result = await CreateSut().CreateOrderAsync(
            new CreateProductionOrderDto("", 0, DateOnly.FromDateTime(DateTime.Today)));

        Assert.False(result.Success);
        Assert.Equal("Producto y cantidad son obligatorios.", result.Message);
        _orders.Verify(r => r.AddAsync(It.IsAny<ProductionOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenProductAndQuantityProvided_CreatesOrder()
    {
        _boms.Setup(r => r.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BomProduct
            {
                ProductName = "Camisa",
                HabilitadoParaOrdenes = true,
                Items =
                [
                    new BomItem
                    {
                        ProductName = "Camisa",
                        MaterialId = 1,
                        Material = new Material { Id = 1, Code = "mat1", Name = "Tela Jersey", Unit = MaterialUnit.Metros },
                        QuantityPerUnit = 1.6m,
                        Unit = MaterialUnit.Metros
                    }
                ]
            });
        _orders.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _orders.Setup(r => r.AddAsync(It.IsAny<ProductionOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ProductionOrder, CancellationToken>((o, _) => o.Id = 1)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateOrderAsync(
            new CreateProductionOrderDto("Camisa", 50, DateOnly.FromDateTime(DateTime.Today.AddDays(7))));

        Assert.True(result.Success);
        Assert.Contains("OP-", result.Message);
        _orders.Verify(r => r.AddAsync(
            It.Is<ProductionOrder>(o => o.ProductName == "Camisa" && o.TotalQuantity == 50),
            It.IsAny<CancellationToken>()), Times.Once);
        _snapshots.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ProductionOrderBomSnapshot>>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
