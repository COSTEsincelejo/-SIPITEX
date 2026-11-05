using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Services;

// Recalcula el costo promedio ponderado del insumo al registrar una compra.
public interface IInsumoCostoService
{
    // stockSinCosto son unidades físicas que no tienen costo conocido. No entran al promedio.
    decimal CalcularNuevoCostoPromedio(
        decimal stockActual,
        decimal costoPromedioActual,
        decimal cantidadComprada,
        decimal precioUnitarioCompra,
        decimal stockSinCosto = 0);

    // Actualiza CostoPromedioPonderado con el stock previo a la entrada. No modifica Stock.
    void RegistrarCompra(Material material, decimal cantidadComprada, decimal precioUnitarioCompra);
}
