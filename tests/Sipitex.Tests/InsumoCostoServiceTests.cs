using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class InsumoCostoServiceTests
{
    private readonly InsumoCostoService _sut = new();

    [Fact]
    public void CalcularNuevoCostoPromedio_PrimeraCompra_UsaPrecioUnitario()
    {
        var promedio = _sut.CalcularNuevoCostoPromedio(
            stockActual: 0,
            costoPromedioActual: 0,
            cantidadComprada: 10,
            precioUnitarioCompra: 12.5m);

        Assert.Equal(12.5m, promedio);
    }

    [Fact]
    public void CalcularNuevoCostoPromedio_DosComprasAPreciosDistintos()
    {
        var despuesPrimera = _sut.CalcularNuevoCostoPromedio(0, 0, 10, 10m);
        var despuesSegunda = _sut.CalcularNuevoCostoPromedio(10, despuesPrimera, 10, 30m);

        Assert.Equal(10m, despuesPrimera);
        Assert.Equal(20m, despuesSegunda);
    }

    [Fact]
    public void CalcularNuevoCostoPromedio_UsaStockPrevioNoElPosterior()
    {
        // (8 * 10 + 2 * 20) / 10 = 12, no (10 * 10 + 2 * 20) / 12
        var promedio = _sut.CalcularNuevoCostoPromedio(8, 10, 2, 20);

        Assert.Equal(12m, promedio);
    }

    [Fact]
    public void CalcularNuevoCostoPromedio_StockAgotado_ReiniciaConPrecioDeCompra()
    {
        var promedio = _sut.CalcularNuevoCostoPromedio(0, 99, 5, 7.5m);

        Assert.Equal(7.5m, promedio);
    }

    [Fact]
    public void RegistrarCompra_NoSumaStock_YActualizaPromedioYUltimoPrecio()
    {
        var material = new Material
        {
            Stock = 10,
            CostoPromedioPonderado = 10,
            CostoAdquisicion = 10
        };

        _sut.RegistrarCompra(material, cantidadComprada: 10, precioUnitarioCompra: 30);

        Assert.Equal(10m, material.Stock);
        Assert.Equal(20m, material.CostoPromedioPonderado);
        Assert.Equal(30m, material.CostoAdquisicion);
    }

    [Fact]
    public void CalcularNuevoCostoPromedio_StockSinCosto_NoDiluyeLaPrimeraCompra()
    {
        // 100 m a $0 (sin costo) + 10 m a $10.000. El promedio usa solo las unidades con costo.
        var promedio = _sut.CalcularNuevoCostoPromedio(
            stockActual: 100,
            costoPromedioActual: 0,
            cantidadComprada: 10,
            precioUnitarioCompra: 10_000m,
            stockSinCosto: 100);

        Assert.Equal(10_000m, promedio);
        Assert.NotEqual(decimal.Round(10_000m * 10 / 110, 4, MidpointRounding.AwayFromZero), promedio);
    }

    [Fact]
    public void RegistrarCompra_LuegoOtraCompra_SigueExcluyendoElStockSinCosto()
    {
        var material = new Material
        {
            Stock = 100,
            StockSinCosto = 100,
            CostoPromedioPonderado = 0
        };

        _sut.RegistrarCompra(material, 10, 10_000m);
        material.Stock = 110;

        Assert.Equal(10_000m, material.CostoPromedioPonderado);
        Assert.Equal(100m, material.StockSinCosto);

        material.DescontarUnidadesSinCosto(20);
        material.Stock = 90;

        _sut.RegistrarCompra(material, 10, 20_000m);

        // Quedan 10 m con costo a 10.000 y entran 10 m a 20.000. Los 80 m sin costo no entran.
        Assert.Equal(15_000m, material.CostoPromedioPonderado);
        Assert.Equal(80m, material.StockSinCosto);
        Assert.Equal(90m, material.Stock);
    }

    [Fact]
    public void RegistrarCompra_PrecioNegativo_Lanza()
    {
        var material = new Material { Stock = 4, CostoPromedioPonderado = 8 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.RegistrarCompra(material, 2, -1));
    }
}
