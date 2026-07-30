using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Resultado de una inspección de calidad sobre una orden
public class QualityRecord
{
    // PK
    public int Id { get; set; }

    // FK de la orden inspeccionada
    public int ProductionOrderId { get; set; }

    // Navegación a la orden (para mostrar OP-xxx en la vista)
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // Cuántas unidades se revisaron
    public int UnitsInspected { get; set; }

    // Resultado: Aprobada / Reproceso / Rechazada
    public QualityResult Result { get; set; }

    // Motivo cuando es Reproceso (null en los otros casos)
    public string? MotivoReproceso { get; set; }

    // Quién inspeccionó (texto libre por ahora)
    public string? Responsable { get; set; }

    // Día de la inspección; por defecto hoy
    public DateOnly InspectionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
