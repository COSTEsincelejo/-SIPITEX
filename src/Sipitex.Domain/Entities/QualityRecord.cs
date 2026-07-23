using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class QualityRecord
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;
    public int UnitsInspected { get; set; }
    public QualityResult Result { get; set; }
    public string? MotivoReproceso { get; set; }
    public string? Responsable { get; set; }
    public DateOnly InspectionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
