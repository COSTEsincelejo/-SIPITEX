using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class ActaMovimiento
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public ActaTipo Tipo { get; set; }

    public ActaOrigen Origen { get; set; }

    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    public string? Observaciones { get; set; }

    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public EstadoProducto? EstadoProductoOrigen { get; set; }
    public EstadoProducto? EstadoProductoDestino { get; set; }

    public string EntregaNombre { get; set; } = string.Empty;
    public string EntregaCargo { get; set; } = string.Empty;
    public DateTime? EntregaConformidadUtc { get; set; }

    public string RecibeNombre { get; set; } = string.Empty;
    public string RecibeCargo { get; set; } = string.Empty;
    public DateTime? RecibeConformidadUtc { get; set; }

    public int CreadoPorUserId { get; set; }
    public User CreadoPor { get; set; } = null!;

    public ICollection<ActaMovimientoDetalle> Detalles { get; set; } = [];
}
