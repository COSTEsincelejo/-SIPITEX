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

    // Cantidad que la orden necesita que bodega entregue
    public decimal QuantityRequired { get; set; }

    // Acumulado realmente entregado por bodega (nunca > QuantityRequired)
    public decimal QuantityDelivered { get; set; }

    // Unidad congelada al asociar (reutiliza MaterialUnit del inventario)
    public MaterialUnit Unit { get; set; }

    public string? Observations { get; set; }

    public decimal QuantityPending => Math.Max(0, QuantityRequired - QuantityDelivered);

    public bool IsFullyDelivered => QuantityDelivered >= QuantityRequired;
}
