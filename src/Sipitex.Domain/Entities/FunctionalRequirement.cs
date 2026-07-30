using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Requisito funcional (RF) del IEEE 830 — para marcar cumplimiento
public class FunctionalRequirement
{
    // PK
    public int Id { get; set; }

    // Código tipo RF01 / RF-01
    public string Code { get; set; } = string.Empty;

    // Qué pide el requisito (texto del documento)
    public string Description { get; set; } = string.Empty;

    // Módulo del sistema al que pertenece (Inventario, Órdenes...)
    public string Module { get; set; } = string.Empty;

    // Cumple / Parcial / Ausente
    public ComplianceStatus Status { get; set; }

    // Nota de por qué está parcial o ausente
    public string Observation { get; set; } = string.Empty;
}
