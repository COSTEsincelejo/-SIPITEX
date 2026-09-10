using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Services;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class MaterialUnitCatalogTests
{
    [Fact]
    public void Catalog_IncludesUniAndUndAsDistinctEntries()
    {
        var names = UnitHelper.Catalog.Select(u => u.ToString()).ToList();
        Assert.Contains("Uni", names);
        Assert.Contains("Und", names);
        Assert.NotEqual(MaterialUnit.Uni, MaterialUnit.Und);
        Assert.Equal("UNI", UnitHelper.ToDisplay(MaterialUnit.Uni));
        Assert.Equal("UND", UnitHelper.ToDisplay(MaterialUnit.Und));
    }

    [Fact]
    public void Catalog_KeepsHistoricalIntegerValues()
    {
        Assert.Equal(0, (int)MaterialUnit.Metros);
        Assert.Equal(1, (int)MaterialUnit.Unidades);
        Assert.Equal(2, (int)MaterialUnit.Kg);
        Assert.Equal(3, (int)MaterialUnit.Gramos);
        Assert.Equal("metro", UnitHelper.ToDisplay(MaterialUnit.Metros));
        Assert.Equal("kilogramo", UnitHelper.ToDisplay(MaterialUnit.Kg));
        Assert.Equal("galón", UnitHelper.ToDisplay(MaterialUnit.Galon));
        Assert.Equal("yarda", UnitHelper.ToDisplay(MaterialUnit.Yarda));
    }
}

public class MaterialConsumptionCostServiceTests
{
    private readonly MaterialConsumptionCostService _sut = new();

    [Fact]
    public void CalcularPromedioPonderado_Empty_ReturnsZero()
    {
        Assert.Equal(0, _sut.CalcularPromedioPonderado([]));
    }

    [Fact]
    public void CalcularPromedioPonderado_WeightsByQuantity()
    {
        var promedio = _sut.CalcularPromedioPonderado(
        [
            new ConsumoCostoLineaDto(10, 20),
            new ConsumoCostoLineaDto(30, 10)
        ]);

        Assert.Equal(12.5m, promedio);
    }

    [Fact]
    public void CalcularPromedioPonderado_IgnoresZeroQuantities()
    {
        var promedio = _sut.CalcularPromedioPonderado(
        [
            new ConsumoCostoLineaDto(0, 99),
            new ConsumoCostoLineaDto(4, 8)
        ]);

        Assert.Equal(8m, promedio);
    }
}
