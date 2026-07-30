using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

// Preferencia de alerta por usuario: si quiere o no recibir cierto tipo de alerta.
public class AlertPreference
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Qué tipo de alerta es (stock bajo, orden atrasada, etc.)
    public AlertType AlertType { get; set; }

    // true = le llegan, false = las silencia
    public bool Enabled { get; set; } = true;
}

// Historial de alertas que ya se enviaron (para no spamear y para auditoría).
public class AlertDelivery
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public AlertType AlertType { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Por ahora solo email, pero lo dejo como string por si después metemos otro canal
    public string Channel { get; set; } = "Email";
}
