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
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMaterialRepository> _materials = new();

    private ProductionOrderService CreateSut() =>
        new(_orders.Object, _boms.Object, _uow.Object, new ProductionConsumptionService(_boms.Object, _materials.Object));

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
        _boms.Setup(r => r.GetByProductAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BomItem
                {
                    ProductName = "Camisa",
                    MaterialId = 1,
                    QuantityPerUnit = 1.6m,
                    Unit = MaterialUnit.Metros
                }
            ]);
        _orders.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateSut().CreateOrderAsync(
            new CreateProductionOrderDto("Camisa", 50, DateOnly.FromDateTime(DateTime.Today.AddDays(7))));

        Assert.True(result.Success);
        Assert.Contains("OP-", result.Message);
        _orders.Verify(r => r.AddAsync(
            It.Is<ProductionOrder>(o => o.ProductName == "Camisa" && o.TotalQuantity == 50),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
