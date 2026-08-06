namespace Sipitex.Domain.Entities;

// Acta de verificación: observación del instructor sobre una orden y firma de aprobación
public class ActaVerificacion
{
    public int Id { get; set; }

    // Orden de producción verificada (ancla principal; alineado con QualityRecord)
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // Ficha/grupo del instructor (para autorización por asignación)
    public int FichaId { get; set; }
    public Ficha Ficha { get; set; } = null!;

    // Instructor que observa / firma
    public int InstructorId { get; set; }
    public User Instructor { get; set; } = null!;

    // Texto libre de la observación
    public string Observacion { get; set; } = string.Empty;

    // Checklist de requisitos (todos deben ser true para firmar)
    public bool CumpleEspecificaciones { get; set; }
    public bool CumpleAcabados { get; set; }
    public bool CumpleSinDefectos { get; set; }

    // Confirmación global de cumplimiento (se sincroniza con el checklist)
    public bool ChecklistCumpleRequisitos { get; set; }

    public DateTime FechaObservacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFirma { get; set; }

    public bool Firmado { get; set; }

    // Snapshot del nombre al firmar (no depende del FK User)
    public string? NombreFirmante { get; set; }

    // Checklist completo y cumplido: ítems + confirmación global
    public bool PuedeFirmarse =>
        !Firmado
        && CumpleEspecificaciones
        && CumpleAcabados
        && CumpleSinDefectos
        && ChecklistCumpleRequisitos;
}
