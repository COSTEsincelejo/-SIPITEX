namespace Sipitex.Domain.Entities;

// Ficha de formación del SENA (grupo/proceso del taller)
public class Ficha
{
    // PK
    public int Id { get; set; }

    // Número de ficha, ej: 2871234 (único en la práctica)
    public string FichaCode { get; set; } = string.Empty;

    // Qué proceso hacen (confección, corte, etc.)
    public string ProcessName { get; set; } = string.Empty;

    // Nombre del instructor en texto (legacy / respaldo si no hay FK)
    public string InstructorName { get; set; } = string.Empty;

    // Turno: mañana, tarde o noche (sirve para filtrar en la vista)
    public string Turno { get; set; } = string.Empty;

    // FK al usuario instructor dueño de la ficha (preferible al nombre)
    public int? InstructorUserId { get; set; }

    // Navegación al User instructor
    public User? InstructorUser { get; set; }

    // FK a la orden en la que trabaja esta ficha (puede ser null al crear)
    public int? ProductionOrderId { get; set; }

    // Navegación a la orden de producción
    public ProductionOrder? ProductionOrder { get; set; }

    // Texto manual cuando la orden no está en ProductionOrders (excluyente con ProductionOrderId)
    public string? AssignedOrderText { get; set; }
}
