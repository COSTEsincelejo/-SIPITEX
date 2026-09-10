namespace Sipitex.Domain.Enums;

// Clasificación de la prenda inspeccionada. Independiente de QualityResult
// (Aprobada / Reproceso / Rechazada), que es el dictamen del flujo de calidad.
public enum CalidadClasificacion
{
    Bueno = 0,
    Regular = 1,
    Malo = 2
}
