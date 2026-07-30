namespace Sipitex.Domain.Entities;

// Ficha de formación / grupo del taller. Cada instructor suele tener la suya.
public class Ficha
{
    public int Id { get; set; }

    // Número de ficha del SENA, ej: 2871234
    public string FichaCode { get; set; } = string.Empty;

    // Nombre del proceso que están haciendo (confección, corte, etc.)
    public string ProcessName { get; set; } = string.Empty;

    // Lo dejé por si no hay usuario vinculado todavía; mejor usar InstructorUserId
    public string InstructorName { get; set; } = string.Empty;

    // Mañana, tarde o noche (lo pedían filtrar en la vista)
    public string Turno { get; set; } = string.Empty;

    // Preferible al nombre: así sé exactamente qué usuario instructor es dueño de la ficha
    public int? InstructorUserId { get; set; }
    public User? InstructorUser { get; set; }

    // Orden de producción a la que está ligada (puede no tener todavía)
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
}
