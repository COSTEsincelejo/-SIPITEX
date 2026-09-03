namespace Sipitex.Domain.Entities;

// Planta de inventario física independiente (Planta 1 / Planta 2) que comparte la misma BD
public class PlantaInventario
{
    // PK
    public int Id { get; set; }

    // Nombre para mostrar (ej. "Planta de Inventario 1")
    public string Nombre { get; set; } = string.Empty;

    // Catálogo de materiales de esta planta
    public ICollection<Material> Materiales { get; set; } = [];

    // Solicitudes multi-ítem dirigidas a esta planta
    public ICollection<SolicitudMaterial> Solicitudes { get; set; } = [];

    // Asignaciones de encargados de bodega (M2M vía UserPlantaInventario)
    public ICollection<UserPlantaInventario> Encargados { get; set; } = [];
}
