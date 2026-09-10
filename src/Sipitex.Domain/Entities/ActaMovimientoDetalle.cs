using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class ActaMovimientoDetalle
{
    public int Id { get; set; }

    public int ActaMovimientoId { get; set; }
    public ActaMovimiento ActaMovimiento { get; set; } = null!;

    public ActaItemTipo ItemTipo { get; set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public int? ConsumoMaterialId { get; set; }
    public ConsumoMaterial? ConsumoMaterial { get; set; }

    public int? StockMovementId { get; set; }
    public StockMovement? StockMovement { get; set; }

    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public string? Unidad { get; set; }
}
