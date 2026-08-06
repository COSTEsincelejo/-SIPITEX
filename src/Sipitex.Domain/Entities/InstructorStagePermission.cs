namespace Sipitex.Domain.Entities;

// Permiso de un instructor sobre una etapa (por nombre de etapa)
public class InstructorStagePermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Nombre de etapa: Trazo, Corte, Confección, …
    public string StageName { get; set; } = string.Empty;
}
