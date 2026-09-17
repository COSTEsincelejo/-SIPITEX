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
    public void RegistrarCompra_PrecioNegativo_Lanza()
    {
        var material = new Material { Stock = 4, CostoPromedioPonderado = 8 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.RegistrarCompra(material, 2, -1));
    }
}
