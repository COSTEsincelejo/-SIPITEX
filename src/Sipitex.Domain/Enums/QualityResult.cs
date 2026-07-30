namespace Sipitex.Domain.Enums;

// Resultado de una inspección de calidad
public enum QualityResult
{
    Aprobada,   // pasó la revisión
    Reproceso,  // hay que rehacer (no se tira del todo)
    Rechazada   // no sirve
}
