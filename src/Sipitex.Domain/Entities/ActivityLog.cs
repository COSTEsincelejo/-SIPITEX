namespace Sipitex.Domain.Entities;

// Registro transversal de actividad de usuarios (auditoría global; no reemplaza OrderChangeLog/StockMovement)
public class ActivityLog
{
    public int Id { get; set; }

    // Quién realizó la acción (sin FK obligatoria: el snapshot UserName sobrevive si se borra el usuario)
    public int UserId { get; set; }

    // Nombre al momento del evento
    public string UserName { get; set; } = string.Empty;

    // Acción corta: CreateUser, DeleteUser, ToggleUserStatus, etc.
    public string Action { get; set; } = string.Empty;

    // Tipo de entidad afectada: User, ProductionOrder, BomProduct...
    public string Entity { get; set; } = string.Empty;

    // PK de la entidad como texto (soporta int/guid/códigos)
    public string? EntityId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Contexto opcional (JSON o texto libre)
    public string? Details { get; set; }
}
