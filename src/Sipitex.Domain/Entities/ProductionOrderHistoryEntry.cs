using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Historial inmutable de la orden (nunca se elimina)
public class ProductionOrderHistoryEntry
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public ProductionHistoryEventType EventType { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }

    public int? StageId { get; set; }
    public string? StageName { get; set; }

    public int? Quantity { get; set; }
}
