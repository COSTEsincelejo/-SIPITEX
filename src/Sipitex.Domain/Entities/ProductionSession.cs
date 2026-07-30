namespace Sipitex.Domain.Entities;

// Un registro de producción de un turno/bloque (lo llena el instructor)
public class ProductionSession
{
    // PK
    public int Id { get; set; }

    // FK a la ficha que produjo
    public int FichaId { get; set; }

    // Navegación a Ficha (null! porque EF siempre la carga si está bien la FK)
    public Ficha Ficha { get; set; } = null!;

    // FK a la orden contra la que se cargó la producción
    public int ProductionOrderId { get; set; }

    // Navegación a la orden
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // Unidades producidas en esta sesión
    public int Units { get; set; }

    // Notas libres del instructor
    public string Observations { get; set; } = string.Empty;

    // Cuándo se registró; UtcNow para no pelear con la zona horaria del server
    public DateTime SessionDate { get; set; } = DateTime.UtcNow;

    // Quién la registró (nullable por datos viejos sin usuario)
    public int? RegisteredByUserId { get; set; }

    // Navegación al usuario que registró
    public User? RegisteredByUser { get; set; }
}
