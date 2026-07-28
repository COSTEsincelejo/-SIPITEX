namespace Sipitex.Domain.Entities;

public class ProductionSession
{
    public int Id { get; set; }
    public int FichaId { get; set; }
    public Ficha Ficha { get; set; } = null!;
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;
    public int Units { get; set; }
    public string Observations { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; } = DateTime.UtcNow;

    /// <summary>Usuario que registró la sesión (instructor o admin).</summary>
    public int? RegisteredByUserId { get; set; }
    public User? RegisteredByUser { get; set; }
}
