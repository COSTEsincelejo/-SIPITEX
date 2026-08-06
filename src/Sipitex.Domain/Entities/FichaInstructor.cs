namespace Sipitex.Domain.Entities;

// Tabla intermedia: una ficha puede tener varios instructores registrados
public class FichaInstructor
{
    public int FichaId { get; set; }
    public Ficha Ficha { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Cuándo se asignó (UTC)
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    // Proceso que realiza este instructor en la ficha (opcional; distinto de Ficha.ProcessName)
    public string? Proceso { get; set; }
}
