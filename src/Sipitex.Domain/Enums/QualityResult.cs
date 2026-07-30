namespace Sipitex.Domain.Enums;

// Resultado de una inspección de calidad
public enum QualityResult
{
    Aprobada,
    Reproceso, // hay que rehacer, no se tira del todo
    Rechazada
}
