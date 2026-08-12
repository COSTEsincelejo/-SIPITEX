namespace Sipitex.Domain.Entities;

// Cabecera de ficha técnica (producto) — metadatos de la receta BOM
public class BomProduct
{
    public int Id { get; set; }

    // Nombre único del producto terminado (ej: "Camisa", "Overol")
    public string ProductName { get; set; } = string.Empty;

    // true = consumos de referencia / pendientes de validar CMTC
    public bool IsReference { get; set; }

    // Observaciones (p. ej. "Valores de referencia, pendientes de validar")
    public string? Notes { get; set; }

    // Si false, CreateOrderAsync rechaza el producto aunque tenga BOM
    public bool HabilitadoParaOrdenes { get; set; } = true;

    public ICollection<BomItem> Items { get; set; } = [];

    // Instructores autorizados a consultar esta ficha técnica en MRP
    public ICollection<BomProductInstructor> Instructors { get; set; } = [];
}
