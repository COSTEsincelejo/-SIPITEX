using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "sipitex@local";
    public string FromName { get; set; } = "SIPITEX Alertas";
    public bool UseSsl { get; set; } = true;
    public string OutboxPath { get; set; } = "email-outbox";
}

public class EmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsSmtpConfigured =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.Host) &&
        !string.IsNullOrWhiteSpace(_options.From);

    public async Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (IsSmtpConfigured)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.From));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.User))
                await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation("Correo enviado a {Email}: {Subject}", toEmail, subject);
            return;
        }

        var dir = Path.GetFullPath(_options.OutboxPath);
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{Sanitize(toEmail)}_{Sanitize(subject)}.txt");
        var content = $"To: {toName} <{toEmail}>\nSubject: {subject}\nSentAt: {DateTime.Now:O}\n\n{body}\n";
        await File.WriteAllTextAsync(file, content, cancellationToken);
        _logger.LogInformation("Correo simulado (outbox) para {Email}: {File}", toEmail, file);
    }

    private static string Sanitize(string value) =>
        string.Concat(value.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')).Truncate(40);
}

file static class StringExtensions
{
    public static string Truncate(this string value, int max) =>
        value.Length <= max ? value : value[..max];
}
