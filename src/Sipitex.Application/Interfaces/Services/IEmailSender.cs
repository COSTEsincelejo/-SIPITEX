namespace Sipitex.Application.Interfaces.Services;

// Abstracción para mandar correos (SMTP real o outbox en desarrollo)
public interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken = default);
    // True si hay servidor SMTP configurado
    bool IsSmtpConfigured { get; }
}
