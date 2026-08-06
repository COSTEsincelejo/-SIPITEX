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

    // Nombres de instructores concatenados (compatibilidad / filtros / reportes)
    public string InstructorName { get; set; } = string.Empty;

    // Turno: mañana, tarde o noche (sirve para filtrar en la vista)
    public string Turno { get; set; } = string.Empty;

    // FK al instructor "principal" (primer asignado); se mantiene sincronizado con Instructors
    public int? InstructorUserId { get; set; }

    // Navegación al instructor principal
    public User? InstructorUser { get; set; }

    // Relación muchos-a-muchos con usuarios Instructor
    public ICollection<FichaInstructor> Instructors { get; set; } = new List<FichaInstructor>();

    // FK a la orden en la que trabaja esta ficha (puede ser null al crear)
    public int? ProductionOrderId { get; set; }

    // Navegación a la orden de producción
    public ProductionOrder? ProductionOrder { get; set; }

    // Texto manual cuando la orden no está en ProductionOrders (excluyente con ProductionOrderId)
    public string? AssignedOrderText { get; set; }
}
