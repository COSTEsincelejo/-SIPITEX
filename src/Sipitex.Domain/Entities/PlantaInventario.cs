namespace Sipitex.Domain.Entities;

// PlantaInventario física independiente (PlantaInventario 1 / PlantaInventario 2) que comparte la misma BD
public class PlantaInventario
{
    // PK
    public int Id { get; set; }

    // Nombre para mostrar (ej. "Planta de Inventario 1")
    public string Nombre { get; set; } = string.Empty;

    // Baja lógica: no se borra físicamente para no romper historial de stock/solicitudes.
    public bool Activo { get; set; } = true;

    // Catálogo de materiales de esta plantaInventario
    public ICollection<Material> Materiales { get; set; } = [];

    // Solicitudes multi-ítem dirigidas a esta plantaInventario
    public ICollection<SolicitudMaterial> Solicitudes { get; set; } = [];

    // Asignaciones de encargados (M2M vía UserPlantaInventario)
    public ICollection<UserPlantaInventario> Encargados { get; set; } = [];
}
