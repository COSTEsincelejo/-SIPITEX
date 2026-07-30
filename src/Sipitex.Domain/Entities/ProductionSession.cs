namespace Sipitex.Domain.Entities;

// Sesión de producción: un registro de cuánto se produjo en un momento dado.
// El instructor la llena cuando termina un turno o un bloque de trabajo.
public class ProductionSession
{
    public int Id { get; set; }

    public int FichaId { get; set; }
    public Ficha Ficha { get; set; } = null!;

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    // Unidades que sacaron en esta sesión
    public int Units { get; set; }

    public string Observations { get; set; } = string.Empty;

    // Uso UtcNow para no pelearme con la zona horaria del servidor
    public DateTime SessionDate { get; set; } = DateTime.UtcNow;

    // Quién la registró (instructor o admin). Nullable por si hay datos viejos sin usuario.
    public int? RegisteredByUserId { get; set; }
    public User? RegisteredByUser { get; set; }
}
