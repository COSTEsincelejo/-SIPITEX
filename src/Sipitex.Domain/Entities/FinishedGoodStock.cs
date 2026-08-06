namespace Sipitex.Domain.Entities;

// Inventario de producto terminado (aparte de materiales/insumos)
public class FinishedGoodStock
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Stock { get; set; }
}

// Movimiento de producto terminado (ingresos parciales, etc.)
public class FinishedGoodMovement
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } // positivo = ingreso
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
    public int? StageId { get; set; }
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public int ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public string? Observations { get; set; }
}
