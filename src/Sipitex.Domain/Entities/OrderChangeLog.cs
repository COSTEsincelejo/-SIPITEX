namespace Sipitex.Domain.Entities;

// Historial de ediciones de campos de una orden (quién, cuándo, qué cambió)
public class OrderChangeLog
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int UsuarioId { get; set; }
    public User Usuario { get; set; } = null!;

    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    // Nombre del campo editado (ej. ProductName, TotalQuantity, Deadline, ClientName)
    public string Campo { get; set; } = string.Empty;

    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
}
