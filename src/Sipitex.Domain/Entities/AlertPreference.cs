using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Preferencia: si el usuario quiere o no cierto tipo de alerta
public class AlertPreference
{
    // PK
    public int Id { get; set; }

    // FK del usuario dueño de la preferencia
    public int UserId { get; set; }

    // Navegación al usuario
    public User User { get; set; } = null!;

    // Tipo de alerta (stock bajo, orden atrasada, etc.)
    public AlertType AlertType { get; set; }

    // true = le llegan; false = las silencia (por defecto activadas)
    public bool Enabled { get; set; } = true;
}

// Historial de una alerta ya enviada (auditoría / outbox)
public class AlertDelivery
{
    // PK
    public int Id { get; set; }

    // A quién se le envió
    public int UserId { get; set; }

    // Navegación al destinatario
    public User User { get; set; } = null!;

    // Qué tipo de alerta era
    public AlertType AlertType { get; set; }

    // Asunto del correo
    public string Subject { get; set; } = string.Empty;

    // Cuerpo del mensaje
    public string Body { get; set; } = string.Empty;

    // Cuándo se envió (o se registró en outbox)
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Canal usado; por ahora "Email" o "SMTP"/"Outbox" según haya config
    public string Channel { get; set; } = "Email";
}
