namespace Sipitex.Domain.Entities;

// Movimiento auditado entre etapas / inventario / retiro (append-only)
public class ProductionOrderStageMovement
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int? FromStageId { get; set; }
    public ProductionOrderStage? FromStage { get; set; }

    public int? ToStageId { get; set; }
    public ProductionOrderStage? ToStage { get; set; }

    // Send | Receive | Withdraw | InventoryIn
    public string MovementType { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;

    public int ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;

    public int? AuthorizedByUserId { get; set; }
    public User? AuthorizedByUser { get; set; }

    public string? Motive { get; set; }
    public string? Observations { get; set; }
}
