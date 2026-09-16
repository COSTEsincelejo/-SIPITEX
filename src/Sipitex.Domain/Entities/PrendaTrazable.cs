using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Unidad de prenda con código único (trazabilidad de producto terminado / en proceso).
public class PrendaTrazable
{
    public int Id { get; set; }

    // Código de negocio único, p. ej. SIP-OP-001-0001
    public string Codigo { get; set; } = string.Empty;

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;

    public string? Talla { get; set; }

    public EstadoProducto Estado { get; set; } = EstadoProducto.MateriaPrima;

    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;

    public int CreadoPorUserId { get; set; }
    public User CreadoPor { get; set; } = null!;

    public string? Observaciones { get; set; }
}
