using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Solicitud de material: producción pide X a bodega
public class MaterialRequest
{
    // PK
    public int Id { get; set; }

    // FK del material pedido
    public int MaterialId { get; set; }

    // Navegación al material
    public Material Material { get; set; } = null!;

    // Cantidad pedida (decimal por si es metros/kg)
    public decimal Quantity { get; set; }

    // FK de la orden que necesita el material
    public int ProductionOrderId { get; set; }

    // Navegación a la orden
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // Usuario que creó la solicitud (null en filas legacy anteriores a este campo)
    public int? SolicitanteId { get; set; }

    // Navegación al solicitante
    public User? Solicitante { get; set; }

    // Estado: arranca Pendiente hasta que bodega apruebe/rechace
    public RequestStatus Status { get; set; } = RequestStatus.Pendiente;

    // Momento en que se creó la solicitud
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
