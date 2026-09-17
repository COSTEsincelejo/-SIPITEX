using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class InventarioConsultaFilterTests
{
    private static MaterialDto Mat(int id, string name, decimal stock, decimal min) =>
        new(
            id,
            name,
            "m",
            MaterialUnit.Metros,
            stock,
            MaterialStatus.Bueno,
            min,
            stock < min,
            new DateOnly(2026, 1, 1),
            0,
            1,
            "Planta 1");

    private static readonly IReadOnlyList<MaterialDto> Sample =
    [
        Mat(1, "Tela Jersey", stock: 20, min: 5),
        Mat(2, "Hilo Poliéster", stock: 3, min: 10),
        Mat(3, "Botón", stock: 0, min: 20),
        Mat(4, "Cremallera", stock: 8, min: 8)
    ];

    [Fact]
    public void Apply_SinFiltros_DevuelveTodos()
    {
        var result = InventarioConsultaFilter.Apply(Sample, null, null);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Apply_PorNombre_EsCaseInsensitiveYParcial()
    {
        var result = InventarioConsultaFilter.Apply(Sample, "  tela  ", null);
        Assert.Single(result);
        Assert.Equal("Tela Jersey", result[0].Name);
    }

    [Fact]
    public void Apply_PorNivelBajo_UsaStockNivelHelper()
    {
        var result = InventarioConsultaFilter.Apply(Sample, null, nameof(StockNivel.Bajo));
        Assert.Single(result);
        Assert.Equal("Hilo Poliéster", result[0].Name);
        Assert.Equal(StockNivel.Bajo, StockNivelHelper.Classify(result[0].Stock, result[0].MinStock));
    }

    [Fact]
    public void Apply_PorNivelCritico_UsaStockNivelHelper()
    {
        var result = InventarioConsultaFilter.Apply(Sample, null, nameof(StockNivel.Critico));
        Assert.Single(result);
        Assert.Equal("Botón", result[0].Name);
        Assert.Equal(StockNivel.Critico, StockNivelHelper.Classify(result[0].Stock, result[0].MinStock));
    }

    [Fact]
    public void Apply_Faltantes_SoloBajoYCritico()
    {
        var result = InventarioConsultaFilter.Apply(Sample, null, InventarioConsultaFilter.Faltantes);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Name == "Hilo Poliéster");
        Assert.Contains(result, m => m.Name == "Botón");
        Assert.DoesNotContain(result, m => m.Name == "Tela Jersey");
        Assert.DoesNotContain(result, m => m.Name == "Cremallera");
    }

    [Fact]
    public void Apply_NombreYNivel_CombinaFiltros()
    {
        var result = InventarioConsultaFilter.Apply(Sample, "o", InventarioConsultaFilter.Faltantes);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Name == "Hilo Poliéster");
        Assert.Contains(result, m => m.Name == "Botón");
    }

    [Fact]
    public void Apply_ListaVacia_NoFalla()
    {
        Assert.Empty(InventarioConsultaFilter.Apply([], "tela", nameof(StockNivel.Bajo)));
        Assert.Empty(InventarioConsultaFilter.Apply(null!, null, null));
    }
}
