using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class InsumoCostoService : IInsumoCostoService
{
    public decimal CalcularNuevoCostoPromedio(
        decimal stockActual,
        decimal costoPromedioActual,
        decimal cantidadComprada,
        decimal precioUnitarioCompra)
    {
        if (cantidadComprada <= 0)
            return stockActual <= 0 ? precioUnitarioCompra : costoPromedioActual;

        if (stockActual <= 0)
            return decimal.Round(precioUnitarioCompra, 4, MidpointRounding.AwayFromZero);

        var denominador = stockActual + cantidadComprada;
        var nuevo = (stockActual * costoPromedioActual + cantidadComprada * precioUnitarioCompra) / denominador;
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
            precioUnitarioCompra);
        material.CostoAdquisicion = precioUnitarioCompra;
    }
}
