using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class GarmentCostingServiceTests
{
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IGrupoConfeccionRepository> _grupos = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<ICostingSettingsService> _rates = new();

    private GarmentCostingService CreateSut(decimal laborHourRate)
    {
        _rates.Setup(s => s.GetLaborHourRateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(laborHourRate);
        return new(
            _consumos.Object,
            _grupos.Object,
            _orders.Object,
            _rates.Object,
            NullLogger<GarmentCostingService>.Instance);
    }

    private void SeedOrder(int volumen = 10)
    {
        _orders.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 10, OrderNumber = "OP-10", TotalQuantity = volumen });
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
    public async Task CalcularAsync_UsaSnapshotDeConsumo_NoElCostoActualDelInsumo()
    {
        SeedOrder(10);
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
                    CostoUnitarioAlMomento = 10,
                    Material = new Material { Id = 3, CostoPromedioPonderado = 25, CostoAdquisicion = 25 }
                }
            ]);

        var result = await CreateSut(5).CalcularAsync(10);

        Assert.Equal(10, result.Volumen);
        Assert.Equal(4, result.CostoMaterialesPorUnidad);
        Assert.Equal(40, result.CostoMateriales);
        Assert.Equal(40, result.Total);
        Assert.Equal(0, result.CostoManoObra);
        Assert.Equal(2, result.HorasManoObra);
        Assert.Contains("costo unitario al momento", result.Formula, StringComparison.Ordinal);
        Assert.Contains("no incluida", result.Formula, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(100, result.CostoMateriales);
        Assert.NotEqual(50, result.Total);
    }

    [Fact]
    public async Task CalcularAsync_PrendaProducidaAntes_NoCambiaSiSubeElPromedioDespues()
    {
        SeedOrder(10);

        _consumos.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial
                {
                    ProductionOrderId = 10,
                    MaterialId = 3,
                    Cantidad = 4,
                    CostoUnitarioAlMomento = 12.5m,
                    Material = new Material { Id = 3, CostoPromedioPonderado = 30 }
                }
            ]);

        var result = await CreateSut(5).CalcularAsync(10);

        Assert.Equal(5, result.CostoMaterialesPorUnidad);
        Assert.Equal(50, result.CostoMateriales);
        Assert.Equal(50, result.Total);
        Assert.Equal(0, result.CostoManoObra);
    }

    [Fact]
    public async Task CalcularAsync_PrendaNueva_UsaSnapshotPosteriorAlCambioDePrecio()
    {
        SeedOrder(10);

        _consumos.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial
                {
                    ProductionOrderId = 10,
                    MaterialId = 3,
                    Cantidad = 4,
                    CostoUnitarioAlMomento = 20
                }
            ]);

        var result = await CreateSut(5).CalcularAsync(10);

        Assert.Equal(8, result.CostoMaterialesPorUnidad);
        Assert.Equal(80, result.CostoMateriales);
        Assert.Equal(80, result.Total);
    }

    [Fact]
    public async Task CalcularAsync_DuplicarVolumenConElMismoCostoUnitario_DuplicaElCostoFinal()
    {
        _grupos.Setup(r => r.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-1", TotalQuantity = 10 });
        _consumos.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial { ProductionOrderId = 1, MaterialId = 3, Cantidad = 20, CostoUnitarioAlMomento = 5 }
            ]);

        _orders.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 2, OrderNumber = "OP-2", TotalQuantity = 20 });
        _consumos.Setup(r => r.GetByOrderIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial { ProductionOrderId = 2, MaterialId = 3, Cantidad = 40, CostoUnitarioAlMomento = 5 }
            ]);

        var chica = await CreateSut(0).CalcularAsync(1);
        var grande = await CreateSut(0).CalcularAsync(2);

        Assert.Equal(10, chica.CostoMaterialesPorUnidad);
        Assert.Equal(10, grande.CostoMaterialesPorUnidad);
        Assert.Equal(100, chica.Total);
        Assert.Equal(200, grande.Total);
        Assert.Equal(chica.Total * 2, grande.Total);
    }

    [Fact]
    public async Task CalcularAsync_ManoDeObraQuedaFueraDelTotal()
    {
        SeedOrder(8);
        _consumos.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial { ProductionOrderId = 10, MaterialId = 1, Cantidad = 8, CostoUnitarioAlMomento = 3 }
            ]);

        var result = await CreateSut(100).CalcularAsync(10);

        Assert.Equal(3, result.CostoMaterialesPorUnidad);
        Assert.Equal(24, result.Total);
        Assert.Equal(0, result.CostoManoObra);
        Assert.Equal(2, result.HorasManoObra);
        Assert.Equal(100, result.TarifaHora);
        Assert.NotEqual(24 + 200, result.Total);
    }

    [Fact]
    public async Task CalcularAsync_OrdenInexistente_CeroConFormula()
    {
        _orders.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionOrder?)null);

        var result = await CreateSut(12).CalcularAsync(99);

        Assert.Equal(99, result.ProductionOrderId);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.Volumen);
        Assert.Equal(12, result.TarifaHora);
        Assert.False(string.IsNullOrWhiteSpace(result.Formula));
    }

    [Fact]
    public async Task CalcularAsync_TarifaNoConfigurada_ManoDeObraSigueExcluida()
    {
        SeedOrder(10);
        _consumos.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateSut(0).CalcularAsync(10);

        Assert.False(result.TarifaConfigurada);
        Assert.Equal(0, result.TarifaHora);
        Assert.Equal(0, result.CostoManoObra);
        Assert.Equal(2, result.HorasManoObra);
        Assert.Equal(0, result.Total);
    }
}
