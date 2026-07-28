namespace Sipitex.Domain.Entities;

public class Ficha
{
    public int Id { get; set; }
    public string FichaCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;

    /// <summary>Usuario con rol Instructor dueño de la ficha (opcional pero preferido frente al nombre).</summary>
    public int? InstructorUserId { get; set; }
    public User? InstructorUser { get; set; }

    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
}
