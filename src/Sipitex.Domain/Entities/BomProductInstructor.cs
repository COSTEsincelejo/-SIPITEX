namespace Sipitex.Domain.Entities;

// Tabla intermedia: una ficha técnica (BomProduct) puede asignarse a varios instructores
public class BomProductInstructor
{
    public int BomProductId { get; set; }
    public BomProduct BomProduct { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Cuándo se asignó (UTC)
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
