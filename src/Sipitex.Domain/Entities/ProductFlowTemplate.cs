using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Plantilla de flujo por producto (Camisa, Pantalón, …) — editable por Admin
public class ProductFlowTemplate
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Name { get; set; } = "Flujo estándar";
    public bool IsActive { get; set; } = true;
    public ICollection<ProductFlowStageTemplate> Stages { get; set; } = [];
}

// Etapa definida en la plantilla (ordenable)
public class ProductFlowStageTemplate
{
    public int Id { get; set; }
    public int ProductFlowTemplateId { get; set; }
    public ProductFlowTemplate Template { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsOptional { get; set; }
}
