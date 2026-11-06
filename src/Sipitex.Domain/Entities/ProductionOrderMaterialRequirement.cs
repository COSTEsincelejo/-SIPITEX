using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Material/insumo opcional asociado a una orden de producción (seleccionado del inventario)
public class ProductionOrderMaterialRequirement
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    // Cantidad que la orden necesita que plantaInventario entregue
    public decimal QuantityRequired { get; set; }

    // Acumulado realmente entregado por plantaInventario (nunca > QuantityRequired)
    public decimal QuantityDelivered { get; set; }

    // Cuánto de lo entregado ya se registró como consumo. El consumo no vuelve a bajar el stock.
    public decimal QuantityConsumed { get; set; }

    // Σ (cantidad de cada entrega × costo unitario de esa entrega). El costo unitario de la
    // entrega es CostoPromedioPonderado en ese momento; las unidades que salen de StockSinCosto entran en 0.
    public decimal CostoEntregadoAcumulado { get; set; }

    // Unidad congelada al asociar (reutiliza MaterialUnit del inventario)
    public MaterialUnit Unit { get; set; }

    public string? Observations { get; set; }

    public decimal QuantityPending => Math.Max(0, QuantityRequired - QuantityDelivered);

    public bool IsFullyDelivered => QuantityDelivered >= QuantityRequired;

    // Promedio de lo entregado. Cero si todavía no hay entrega o si todo salió de stock sin costo.
    public decimal CostoUnitarioEntrega =>
        QuantityDelivered <= 0 ? 0 : CostoEntregadoAcumulado / QuantityDelivered;

    // El reingreso desde etapas no descuenta esta cifra (docs/pendiente-reingreso-consumo.md).
    public decimal QuantityAvailableToConsume => Math.Max(0, QuantityDelivered - QuantityConsumed);
}
