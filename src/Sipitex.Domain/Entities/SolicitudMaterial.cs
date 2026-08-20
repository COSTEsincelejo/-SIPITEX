using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Solicitud multi-ítem: PorFicha (SENA + catálogo) o InsumosLibres (descripción libre)
public class SolicitudMaterial
{
    public int Id { get; set; }

    // Consecutivo autogenerado, ej. SOL-0001
    public string Codigo { get; set; } = string.Empty;

    // PorFicha (histórico) o InsumosLibres (descripción)
    public SolicitudMaterialTipo Tipo { get; set; } = SolicitudMaterialTipo.PorFicha;

    // Obligatorio solo si Tipo == PorFicha; opcional en InsumosLibres
    public int? FichaId { get; set; }
    public Ficha? Ficha { get; set; }

    // Opcional (típicamente InsumosLibres); independiente de FichaId
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    // Texto libre de cabecera (InsumosLibres); null en PorFicha legacy
    public string? DescripcionLibre { get; set; }

    // Instructor o Administrador que solicita
    public int SolicitanteId { get; set; }
    public User Solicitante { get; set; } = null!;

    public SolicitudMaterialEstado Estado { get; set; } = SolicitudMaterialEstado.Pendiente;

    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

    public DateTime? FechaResolucion { get; set; }

    // Bodeguero (u otro actor) que resolvió la solicitud
    public int? ResueltoPorId { get; set; }
    public User? ResueltoPor { get; set; }

    public string? Observaciones { get; set; }

    // FK obligatoria a la bodega que atiende esta solicitud (default 1 = Bodega 1, alineado al backfill)
    public int BodegaId { get; set; } = 1;

    // Navegación a la bodega
    public Bodega Bodega { get; set; } = null!;

    public ICollection<DetalleSolicitudMaterial> Detalles { get; set; } = new List<DetalleSolicitudMaterial>();

    // 1:1 por ahora — una entrega por solicitud resuelta
    public EntregaMaterial? Entrega { get; set; }
}
