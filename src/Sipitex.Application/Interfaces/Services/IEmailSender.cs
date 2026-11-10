namespace Sipitex.Application.Interfaces.Services;

// Único punto de envío. La implementación elige Resend, Brevo, SMTP u outbox.
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

public interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken = default);

    Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken = default);
    // True si hay servidor SMTP configurado (host, remitente y usuario).
    bool IsSmtpConfigured { get; }
    // Resend, Brevo, Smtp u Outbox. Vacío en dobles de prueba que no lo definen.
    string DeliveryChannel { get; }
}
