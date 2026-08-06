using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Instancia de etapa en una orden concreta (copia editable del flujo)
public class ProductionOrderStage
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsOptional { get; set; }

    public ProductionStageStatus Status { get; set; } = ProductionStageStatus.Pendiente;

    public int? InstructorUserId { get; set; }
    public User? InstructorUser { get; set; }

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Observations { get; set; }

    public int QuantityReceived { get; set; }
    public int QuantityProcessed { get; set; }
    public int QuantitySent { get; set; }
    public int QuantityWithdrawn { get; set; }

    // WIP disponible en la etapa para enviar / retirar / ingresar a inventario
    public int QuantityAvailable =>
        Math.Max(0, QuantityReceived - QuantitySent - QuantityWithdrawn);
}
