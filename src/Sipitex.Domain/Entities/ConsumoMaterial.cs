namespace Sipitex.Domain.Entities;

// Ficha de consumo: lo usado en una orden. No descuenta stock.
// El stock sale en la entrega de la orden (ProductionOrderMaterialRequirement).
// EntregaMaterial es otra cosa: cierra una SolicitudMaterial.
public class ConsumoMaterial
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int? GrupoConfeccionId { get; set; }
    public GrupoConfeccion? GrupoConfeccion { get; set; }

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    // Línea de entrega de la orden de la que sale este uso. Nulo en consumos anteriores a la regla.
    public int? OrderMaterialRequirementId { get; set; }
    public ProductionOrderMaterialRequirement? OrderMaterialRequirement { get; set; }

    public decimal Cantidad { get; set; }

    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    public int ResponsableUserId { get; set; }
    public User Responsable { get; set; } = null!;

    // Snapshot del costo promedio ponderado del insumo al registrar el consumo. No se recalcula después.
    public decimal CostoUnitarioAlMomento { get; set; }
}
