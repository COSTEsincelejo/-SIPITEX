using Moq;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class MrpServiceTests
{
    private readonly Mock<IBomRepository> _bomRepository = new();
    private readonly Mock<IMaterialRepository> _materialRepository = new();

    private MrpService CreateSut() => new(_bomRepository.Object, _materialRepository.Object);

    [Fact]
    public async Task SimulateAsync_WhenStockIsSufficient_ReportsNoDeficit()
    {
        var tela = new Material { Id = 1, Name = "Tela", Stock = 100, Unit = MaterialUnit.Metros };
        _bomRepository
            .Setup(r => r.GetByProductAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BomItem
                {
                    ProductName = "Camisa",
                    MaterialId = 1,
                    Material = tela,
                    QuantityPerUnit = 1.5m,
                    Unit = MaterialUnit.Metros
                }
            ]);
        _materialRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tela);

        var result = await CreateSut().SimulateAsync("Camisa", 10);

        Assert.Equal("Camisa", result.ProductName);
        Assert.Equal(10, result.Quantity);
        var line = Assert.Single(result.Lines);
        Assert.Equal(15m, line.Required);
        Assert.Equal(100m, line.Available);
        Assert.Equal(0m, line.Deficit);
        Assert.True(line.IsOk);
    }

    [Fact]
    public async Task SimulateAsync_WhenStockIsInsufficient_ReportsDeficit()
    {
        var hilo = new Material { Id = 2, Name = "Hilo", Stock = 5, Unit = MaterialUnit.Unidades };
        _bomRepository
            .Setup(r => r.GetByProductAsync("Pantalón", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new BomItem
                {
                    ProductName = "Pantalón",
                    MaterialId = 2,
                    Material = hilo,
                    QuantityPerUnit = 2m,
                    Unit = MaterialUnit.Unidades
                }
            ]);
        _materialRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hilo);

        var result = await CreateSut().SimulateAsync("Pantalón", 10);

        var line = Assert.Single(result.Lines);
        Assert.Equal(20m, line.Required);
        Assert.Equal(5m, line.Available);
        Assert.Equal(15m, line.Deficit);
        Assert.False(line.IsOk);
    }

    [Fact]
    public async Task SimulateAsync_WhenBomIsEmpty_ReturnsNoLines()
    {
        _bomRepository
            .Setup(r => r.GetByProductAsync("Chaqueta", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateSut().SimulateAsync("Chaqueta", 5);

        Assert.Equal("Chaqueta", result.ProductName);
        Assert.Equal(5, result.Quantity);
        Assert.Empty(result.Lines);
    }
}
