using Microsoft.Extensions.Options;
using Moq;
using Sipitex.Application;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class GarmentCostingServiceTests
{
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IGrupoConfeccionRepository> _grupos = new();
    private readonly Mock<IStockMovementRepository> _stock = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();

    private GarmentCostingService CreateSut(decimal laborHourRate) => new(
        _consumos.Object,
        _grupos.Object,
        _stock.Object,
        _orders.Object,
        Options.Create(new CostingOptions { LaborHourRate = laborHourRate }),
        NullLogger<GarmentCostingService>.Instance);

    private void SeedOrderAndGrupo()
    {
        _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 10, OrderNumber = "OP-10" });
        _grupos.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new GrupoConfeccion
                {
                    Id = 1,
                    ProductionOrderId = 10,
                    HoraInicio = new TimeOnly(8, 0),
                    HoraFin = new TimeOnly(10, 0),
                    CantidadPrendas = 12
                }
            ]);
    }

    [Fact]
    public async Task CalcularAsync_SinCambioDePrecio_UsaCostoDeEntrada()
    {
        SeedOrderAndGrupo();
        var compra = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var uso = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

        _consumos.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial
                {
                    ProductionOrderId = 10,
                    MaterialId = 3,
                    Cantidad = 4,
                    FechaUtc = uso,
                    CostoUnitario = 10
                }
            ]);
        _stock.Setup(r => r.QueryAsync(null, uso, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new StockMovement
                {
                    MaterialId = 3,
                    FechaUtc = compra,
                    TipoMovimiento = StockMovementType.Entrada,
                    CostoUnitario = 10,
                    Cantidad = 50
                }
            ]);

        var result = await CreateSut(5).CalcularAsync(10);

        Assert.Equal(40, result.CostoMateriales);
        Assert.Equal(2, result.HorasManoObra);
        Assert.Equal(5, result.TarifaHora);
        Assert.Equal(10, result.CostoManoObra);
        Assert.Equal(50, result.Total);
        Assert.Contains("Costing:LaborHourRate", result.Formula, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CalcularAsync_ConCambioDePrecioEntreCompraYUso_UsaCostoHistoricoDeEntrada()
    {
        SeedOrderAndGrupo();
        var compra = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var uso = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

        // El catálogo / snapshot del consumo ya refleja el precio nuevo (25),
        // pero la única entrada previa al uso sigue costando 10.
        _consumos.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial
                {
                    ProductionOrderId = 10,
                    MaterialId = 3,
                    Cantidad = 4,
                    FechaUtc = uso,
                    CostoUnitario = 25
                }
            ]);
        _stock.Setup(r => r.QueryAsync(null, uso, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new StockMovement
                {
                    MaterialId = 3,
                    FechaUtc = compra,
                    TipoMovimiento = StockMovementType.Entrada,
                    CostoUnitario = 10,
                    Cantidad = 50
                }
            ]);

        var result = await CreateSut(5).CalcularAsync(10);

        Assert.Equal(40, result.CostoMateriales);
        Assert.Equal(10, result.CostoManoObra);
        Assert.Equal(50, result.Total);
        Assert.NotEqual(100, result.CostoMateriales);
    }

    [Fact]
    public async Task CalcularAsync_OrdenInexistente_CeroConFormula()
    {
        _orders.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionOrder?)null);

        var result = await CreateSut(12).CalcularAsync(99);

        Assert.Equal(99, result.ProductionOrderId);
        Assert.Equal(0, result.Total);
        Assert.Equal(12, result.TarifaHora);
        Assert.False(string.IsNullOrWhiteSpace(result.Formula));
    }

    [Fact]
    public async Task CalcularAsync_TarifaNoConfigurada_ManoDeObraEnCeroYFlag()
    {
        SeedOrderAndGrupo();
        _consumos.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _stock.Setup(r => r.QueryAsync(null, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateSut(0).CalcularAsync(10);

        Assert.False(result.TarifaConfigurada);
        Assert.Equal(0, result.TarifaHora);
        Assert.Equal(0, result.CostoManoObra);
        Assert.Equal(2, result.HorasManoObra);
        Assert.Equal(0, result.Total);
    }
}
