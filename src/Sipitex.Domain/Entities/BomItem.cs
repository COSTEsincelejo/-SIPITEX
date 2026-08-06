using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Ítem del BOM: cuánto material sale por cada unidad del producto
public class BomItem
{
    // PK
    public int Id { get; set; }

    // FK a la cabecera de ficha técnica
    public int BomProductId { get; set; }
    public BomProduct BomProduct { get; set; } = null!;

    // Nombre del producto (denormalizado; se mantiene alineado con BomProduct.ProductName)
    public string ProductName { get; set; } = string.Empty;

    // FK al material que se consume
    public int MaterialId { get; set; }

    // Navegación al material
    public Material Material { get; set; } = null!;

    // Cuánto se gasta de ese material por 1 unidad fabricada
    public decimal QuantityPerUnit { get; set; }

    // Unidad del material en esta receta (metros, kg, etc.)
    public MaterialUnit Unit { get; set; }
}
