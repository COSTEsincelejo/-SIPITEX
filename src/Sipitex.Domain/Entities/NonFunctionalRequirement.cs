using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Requisito no funcional (RNF): rendimiento, seguridad, usabilidad, etc.
// Igual que los funcionales, sirve para el seguimiento del IEEE 830.
public class NonFunctionalRequirement
{
    public int Id { get; set; }

    // Código tipo RNF-01...
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ComplianceStatus Status { get; set; }

    public string Observation { get; set; } = string.Empty;
}
