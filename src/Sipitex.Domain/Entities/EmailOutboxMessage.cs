namespace Sipitex.Domain.Entities;

// Correo que no pudo salir por SMTP. Sustituye la carpeta email-outbox del disco.
public class EmailOutboxMessage
{
    public int Id { get; set; }

    public string ToEmail { get; set; } = string.Empty;

    public string ToName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
