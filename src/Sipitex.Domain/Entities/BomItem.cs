using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Ítem del BOM (Bill of Materials): cuánto material se necesita por unidad de producto.
// El MRP usa esto para calcular qué falta.
public class BomItem
{
    public int Id { get; set; }

    // Nombre del producto terminado (ej: "Camisa polo")
    public string ProductName { get; set; } = string.Empty;

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    // Cantidad de material por cada unidad que se fabrique
    public decimal QuantityPerUnit { get; set; }

    // Unidad del material (debe coincidir con la del Material, o al menos ser coherente)
    public MaterialUnit Unit { get; set; }
}
