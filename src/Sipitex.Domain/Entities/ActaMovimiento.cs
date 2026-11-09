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

    public int? PlantaOrigenId { get; set; }
    public PlantaInventario? PlantaOrigen { get; set; }

    public int? PlantaDestinoId { get; set; }
    public PlantaInventario? PlantaDestino { get; set; }

    public ActaEstado Estado { get; set; } = ActaEstado.Firmada;

    public DateTime? AnuladaUtc { get; set; }
    public int? AnuladaPorUserId { get; set; }
    public User? AnuladaPor { get; set; }
    public string? MotivoAnulacion { get; set; }

    public string EntregaNombre { get; set; } = string.Empty;
    public string EntregaCargo { get; set; } = string.Empty;
    public DateTime? EntregaConformidadUtc { get; set; }
    public byte[]? EntregaFirmaPng { get; set; }

    public string RecibeNombre { get; set; } = string.Empty;
    public string RecibeCargo { get; set; } = string.Empty;
    public DateTime? RecibeConformidadUtc { get; set; }
    public byte[]? RecibeFirmaPng { get; set; }

    public int CreadoPorUserId { get; set; }
    public User CreadoPor { get; set; } = null!;

    public ICollection<ActaMovimientoDetalle> Detalles { get; set; } = [];
}
