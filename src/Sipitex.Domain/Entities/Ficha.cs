namespace Sipitex.Domain.Entities;

public class Ficha
{
    public int Id { get; set; }
    public string FichaCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
}
