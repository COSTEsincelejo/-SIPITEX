using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Requisito funcional (RF) del documento IEEE 830.
// Lo usamos para marcar si el sistema ya cumple o no cada requisito.
public class FunctionalRequirement
{
    public int Id { get; set; }

    // Código tipo RF-01, RF-02...
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Módulo al que pertenece (Inventario, Órdenes, etc.)
    public string Module { get; set; } = string.Empty;

    // Cumple / Parcial / Ausente
    public ComplianceStatus Status { get; set; }

    // Nota libre de por qué está parcial o ausente
    public string Observation { get; set; } = string.Empty;
}
