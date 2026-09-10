namespace Sipitex.Domain.Entities;

// Ficha de consumo: material usado en una orden (y, más adelante, en un grupo de confección).
public class ConsumoMaterial
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // TODO PR grupos de confección: FK opcional hasta que exista GrupoConfeccion.
    public int? GrupoConfeccionId { get; set; }

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public decimal Cantidad { get; set; }

    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    public int ResponsableUserId { get; set; }
    public User Responsable { get; set; } = null!;

    // Costo unitario de adquisición al momento del consumo (promedio ponderado / costo vigente).
    public decimal CostoUnitario { get; set; }
}
