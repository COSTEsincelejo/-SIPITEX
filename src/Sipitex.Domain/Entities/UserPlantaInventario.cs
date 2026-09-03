namespace Sipitex.Domain.Entities;

// Tabla puente User ↔ PlantaInventario: un encargado de bodega puede estar en varias plantas.
public class UserPlantaInventario
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int PlantaInventarioId { get; set; }
    public PlantaInventario PlantaInventario { get; set; } = null!;
}
