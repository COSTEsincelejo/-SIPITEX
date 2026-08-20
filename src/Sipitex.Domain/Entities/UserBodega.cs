namespace Sipitex.Domain.Entities;

// Tabla puente User ↔ Bodega: un bodeguero puede encargarse de varias bodegas.
public class UserBodega
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BodegaId { get; set; }
    public Bodega Bodega { get; set; } = null!;
}
