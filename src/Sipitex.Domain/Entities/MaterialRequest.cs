using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Solicitud de material: producción pide X cantidad a bodega.
// El bodeguero la aprueba o rechaza.
public class MaterialRequest
{
    public int Id { get; set; }

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    // Cuánto están pidiendo
    public decimal Quantity { get; set; }

    // Siempre va ligada a una orden de producción
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // Empieza pendiente hasta que bodega la revise
    public RequestStatus Status { get; set; } = RequestStatus.Pendiente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
