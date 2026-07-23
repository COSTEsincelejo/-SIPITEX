using Sipitex.Domain.Enums;

namespace Sipitex.Domain.Entities;

public class AlertPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public AlertType AlertType { get; set; }
    public bool Enabled { get; set; } = true;
}

public class AlertDelivery
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public AlertType AlertType { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string Channel { get; set; } = "Email";
}
