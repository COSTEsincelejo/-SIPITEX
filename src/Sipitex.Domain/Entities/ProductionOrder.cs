using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class ProductionOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int ProducedQuantity { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.EnProceso;
    public DateOnly Deadline { get; set; }

    public ICollection<MaterialRequest> MaterialRequests { get; set; } = [];
    public ICollection<QualityRecord> QualityRecords { get; set; } = [];
    public ICollection<Ficha> Fichas { get; set; } = [];
}
