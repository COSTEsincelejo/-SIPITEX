namespace Sipitex.Domain.Entities;

// Tabla puente User ↔ PlantaInventario: un encargadoDeBodega puede encargarse de varias plantasInventario.
public class UserPlantaInventario
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int PlantaInventarioId { get; set; }
    public PlantaInventario PlantaInventario { get; set; } = null!;
}
