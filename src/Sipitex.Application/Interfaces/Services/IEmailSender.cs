namespace Sipitex.Application.Interfaces.Services;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken = default);
    bool IsSmtpConfigured { get; }
}
