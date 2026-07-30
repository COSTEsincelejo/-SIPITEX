using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Requisito no funcional (RNF): seguridad, usabilidad, etc.
public class NonFunctionalRequirement
{
    // PK
    public int Id { get; set; }

    // Código tipo RNF01
    public string Code { get; set; } = string.Empty;

    // Descripción del requisito
    public string Description { get; set; } = string.Empty;

    // Cumple / Parcial / Ausente
    public ComplianceStatus Status { get; set; }

    // Nota libre del estado
    public string Observation { get; set; } = string.Empty;
}
