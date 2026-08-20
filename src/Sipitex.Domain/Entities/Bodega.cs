namespace Sipitex.Domain.Entities;

// Bodega física independiente (Bodega 1 / Bodega 2) que comparte la misma BD
public class Bodega
{
    // PK
    public int Id { get; set; }

    // Nombre para mostrar (ej. "Bodega 1")
    public string Nombre { get; set; } = string.Empty;

    // Catálogo de materiales de esta bodega
    public ICollection<Material> Materiales { get; set; } = [];

    // Solicitudes multi-ítem dirigidas a esta bodega
    public ICollection<SolicitudMaterial> Solicitudes { get; set; } = [];

    // Asignaciones de bodegueros (M2M vía UserBodega)
    public ICollection<UserBodega> Bodegueros { get; set; } = [];
}
