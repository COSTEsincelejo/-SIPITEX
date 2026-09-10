using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class GrupoConfeccion
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int InstructorUserId { get; set; }
    public User Instructor { get; set; } = null!;

    public DateOnly FechaRealizacion { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly? HoraFin { get; set; }

    public int CantidadPrendas { get; set; }

    public ICollection<ConsumoMaterial> Consumos { get; set; } = [];
}
