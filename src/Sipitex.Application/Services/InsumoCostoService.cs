using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class InsumoCostoService : IInsumoCostoService
{
    public decimal CalcularNuevoCostoPromedio(
        decimal stockActual,
        decimal costoPromedioActual,
        decimal cantidadComprada,
        decimal precioUnitarioCompra,
        decimal stockSinCosto = 0)
    {
        if (cantidadComprada <= 0)
            return stockActual <= 0 ? precioUnitarioCompra : costoPromedioActual;

        // 100 m sin costo + 10 m a 10.000 → promedio 10.000, no (0×100 + 10.000×10) / 110.
        var sinCosto = stockSinCosto < 0 ? 0 : stockSinCosto;
        if (stockActual <= 0)
            sinCosto = 0;
        else if (sinCosto > stockActual)
            sinCosto = stockActual;
        var stockConCosto = stockActual - sinCosto;

        if (stockConCosto <= 0)
            return decimal.Round(precioUnitarioCompra, 4, MidpointRounding.AwayFromZero);

        var denominador = stockConCosto + cantidadComprada;
        var nuevo = (stockConCosto * costoPromedioActual + cantidadComprada * precioUnitarioCompra) / denominador;
        return decimal.Round(nuevo, 4, MidpointRounding.AwayFromZero);
    }

    public void RegistrarCompra(Material material, decimal cantidadComprada, decimal precioUnitarioCompra)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (precioUnitarioCompra < 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitarioCompra), "El precio unitario de compra no puede ser negativo.");

        material.CostoPromedioPonderado = CalcularNuevoCostoPromedio(
            material.Stock,
            material.CostoPromedioPonderado,
            cantidadComprada,
            precioUnitarioCompra,
            material.StockSinCosto);
        material.CostoAdquisicion = precioUnitarioCompra;
    }
}
