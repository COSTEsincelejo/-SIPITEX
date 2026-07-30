using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Registro de inspección de calidad de una orden.
public class QualityRecord
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // Cuántas unidades se revisaron en esta inspección
    public int UnitsInspected { get; set; }

    // Aprobada, Reproceso o Rechazada
    public QualityResult Result { get; set; }

    // Solo tiene sentido cuando el resultado es Reproceso (por qué hay que rehacer)
    public string? MotivoReproceso { get; set; }

    // Quién hizo la inspección (nombre libre por ahora)
    public string? Responsable { get; set; }

    public DateOnly InspectionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
