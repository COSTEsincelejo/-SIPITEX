using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class FunctionalRequirement
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public ComplianceStatus Status { get; set; }
    public string Observation { get; set; } = string.Empty;
}
