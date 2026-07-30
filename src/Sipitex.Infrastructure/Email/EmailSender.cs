using MailKit.Net.Smtp; // Cliente SMTP para mandar correos de verdad
using MailKit.Security; // StartTls, SSL...
using Microsoft.Extensions.Logging; // Para loguear si mandó o simuló
using Microsoft.Extensions.Options; // Lee EmailOptions del appsettings
using MimeKit; // Arma el mensaje MIME (asunto, cuerpo, destinatario)
using Sipitex.Application.Interfaces.Services; // IEmailSender

namespace Sipitex.Infrastructure.Email;

// Opciones del correo, vienen de appsettings sección "Email"
public class EmailOptions
{
    public const string SectionName = "Email"; // Nombre de la sección en appsettings
    public bool Enabled { get; set; } // Si está apagado, va directo al outbox
    public string Host { get; set; } = string.Empty; // Servidor SMTP (ej. smtp.gmail.com)
    public int Port { get; set; } = 587; // Puerto típico con STARTTLS
    public string User { get; set; } = string.Empty; // Usuario SMTP si pide auth
    public string Password { get; set; } = string.Empty; // Contraseña o app password
    public string From { get; set; } = "sipitex@local"; // Remitente del correo
    public string FromName { get; set; } = "SIPITEX Alertas"; // Nombre que ve el destinatario
    public bool UseSsl { get; set; } = true; // Usar TLS al conectar
    // Si no hay SMTP, guardo los correos acá como .txt (útil en desarrollo)
    public string OutboxPath { get; set; } = "email-outbox";
}

// Implementación concreta de IEmailSender
public class EmailSender : IEmailSender
{
    private readonly EmailOptions _options; // Config leída una vez
    private readonly ILogger<EmailSender> _logger; // Para dejar rastro en consola

    // El DI inyecta opciones y logger
    public EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value; // .Value saca el objeto de IOptions
        _logger = logger;
    }

    // Reviso si hay servidor SMTP configurado o toca simular
    public bool IsSmtpConfigured =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.Host) &&
        !string.IsNullOrWhiteSpace(_options.From);

    // Manda el correo por SMTP o lo guarda en archivo
    public async Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (IsSmtpConfigured)
        {
            var message = new MimeMessage(); // Mensaje vacío de MailKit
            message.From.Add(new MailboxAddress(_options.FromName, _options.From)); // Quién envía
            message.To.Add(new MailboxAddress(toName, toEmail)); // A quién va
            message.Subject = subject; // Asunto
            message.Body = new TextPart("plain") { Text = body }; // Cuerpo en texto plano

            using var client = new SmtpClient(); // Cliente SMTP desechable
            await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.User))
                await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken); // Login si hay credenciales
            await client.SendAsync(message, cancellationToken); // Envío real
            await client.DisconnectAsync(true, cancellationToken); // Cierro bien la conexión
            _logger.LogInformation("Correo enviado a {Email}: {Subject}", toEmail, subject);
            return; // Listo, salgo
        }

        // Fallback: escribo el correo a un archivo en vez de mandarlo
        var dir = Path.GetFullPath(_options.OutboxPath); // Ruta absoluta de la carpeta outbox
        Directory.CreateDirectory(dir); // La creo si no existe
        var file = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{Sanitize(toEmail)}_{Sanitize(subject)}.txt"); // Nombre único por timestamp
        var content = $"To: {toName} <{toEmail}>\nSubject: {subject}\nSentAt: {DateTime.Now:O}\n\n{body}\n"; // Contenido legible
        await File.WriteAllTextAsync(file, content, cancellationToken);
        _logger.LogInformation("Correo simulado (outbox) para {Email}: {File}", toEmail, file);
    }

    // Quito caracteres raros del nombre del archivo
    private static string Sanitize(string value) =>
        string.Concat(value.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')).Truncate(40);
}

// Helper chiquito para acortar strings en el nombre del archivo
file static class StringExtensions
{
    public static string Truncate(this string value, int max) =>
        value.Length <= max ? value : value[..max]; // Corto si se pasa del límite
}
