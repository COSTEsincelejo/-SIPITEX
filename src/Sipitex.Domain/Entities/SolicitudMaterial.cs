using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Solicitud multi-ítem de materiales ligada a una Ficha (flujo paralelo a MaterialRequest)
public class SolicitudMaterial
{
    public int Id { get; set; }

    // Consecutivo autogenerado, ej. SOL-0001
    public string Codigo { get; set; } = string.Empty;

    public int FichaId { get; set; }
    public Ficha Ficha { get; set; } = null!;

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

    public ICollection<DetalleSolicitudMaterial> Detalles { get; set; } = new List<DetalleSolicitudMaterial>();

    // 1:1 por ahora — una entrega por solicitud resuelta
    public EntregaMaterial? Entrega { get; set; }
}
