using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class Material
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MaterialUnit Unit { get; set; }
    public decimal Stock { get; set; }
    public decimal MinStock { get; set; }
    public MaterialStatus Status { get; set; } = MaterialStatus.Bueno;

    public ICollection<BomItem> BomItems { get; set; } = [];
    public ICollection<MaterialRequest> Requests { get; set; } = [];
}
