using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Receta congelada al crear la orden — ConsumeAsync / hint MRP usan esto, no el BOM vivo
public class ProductionOrderBomSnapshot
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // FK al material (Restrict: no borrar material si hay snapshot)
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    // Denormalizado al momento de crear la orden (el catálogo puede renombrarse después)
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;

    public decimal QuantityPerUnit { get; set; }
    public MaterialUnit Unit { get; set; }
}
