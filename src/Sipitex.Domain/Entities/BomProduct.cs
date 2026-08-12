namespace Sipitex.Domain.Entities;

// Cabecera de ficha técnica (producto) — metadatos de la receta BOM
// Fase A: metadatos base CMTC + tallas (opcionales; fichas legacy quedan en null/vacío)
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

    // --- Fase A: metadatos base (todos opcionales) ---
    public string? Referencia { get; set; }
    public string? Linea { get; set; }
    public string? TallaInicial { get; set; }
    public string? TipoEmpaque { get; set; }
    public string? DescripcionPrenda { get; set; }
    public DateOnly? FechaSolicitud { get; set; }
    public DateOnly? FechaElaboracion { get; set; }
    public int? AnioMuestrario { get; set; }
    public bool EsDisenoNuevo { get; set; }
    public bool EsReplica { get; set; }
    public bool EsBancoDeMuestras { get; set; }
    public string? Disenador { get; set; }
    public string? Patronista { get; set; }
    public string? Digitacion { get; set; }

    public ICollection<BomItem> Items { get; set; } = [];

    // Instructores autorizados a consultar esta ficha técnica en MRP
    public ICollection<BomProductInstructor> Instructors { get; set; } = [];

    // Tallas de la ficha (Fase A)
    public ICollection<BomProductTalla> Tallas { get; set; } = [];

    // Piezas del patrón y tablas de medidas (Fase B)
    public ICollection<BomProductPieza> Piezas { get; set; } = [];
    public ICollection<BomProductMedida> Medidas { get; set; } = [];
}
