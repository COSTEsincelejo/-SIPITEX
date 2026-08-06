namespace Sipitex.Domain.Entities;

// Entrega asociada a una SolicitudMaterial ya resuelta (1:1 por ahora)
public class EntregaMaterial
{
    public int Id { get; set; }

    // Consecutivo autogenerado, ej. ENT-0001
    public string Codigo { get; set; } = string.Empty;

    public int SolicitudMaterialId { get; set; }
    public SolicitudMaterial SolicitudMaterial { get; set; } = null!;

    public int BodegueroId { get; set; }
    public User Bodeguero { get; set; } = null!;

    public DateTime FechaEntrega { get; set; } = DateTime.UtcNow;

    public string? Observaciones { get; set; }
}
