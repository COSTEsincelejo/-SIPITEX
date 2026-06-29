using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class BomItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public decimal QuantityPerUnit { get; set; }
    public MaterialUnit Unit { get; set; }
}
