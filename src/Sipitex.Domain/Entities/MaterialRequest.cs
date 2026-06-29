using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class MaterialRequest
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public decimal Quantity { get; set; }
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;
    public RequestStatus Status { get; set; } = RequestStatus.Pendiente;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
