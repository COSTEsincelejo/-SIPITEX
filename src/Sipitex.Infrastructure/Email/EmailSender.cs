using MailKit.Net.Smtp; // Cliente SMTP para mandar correos de verdad
using MailKit.Security; // StartTls, SSL...
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Para loguear si mandó o simuló
using Microsoft.Extensions.Options; // Lee EmailOptions del appsettings
using MimeKit; // Arma el mensaje MIME (asunto, cuerpo, destinatario)
using Sipitex.Application.Interfaces.Services; // IEmailSender
using Sipitex.Infrastructure.Persistence;

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
    // Ya no se usa: el fallback vive en EmailOutboxMessages. Se deja para no romper appsettings viejos.
    public string OutboxPath { get; set; } = "email-outbox";
}

// Implementación concreta de IEmailSender
public class EmailSender : IEmailSender
{
    private readonly EmailOptions _options; // Config leída una vez
    private readonly ILogger<EmailSender> _logger; // Para dejar rastro en consola
    private readonly SipitexDbContext? _db;

    // El DI inyecta opciones, logger y el contexto. Los tests de configuración no pasan contexto.
    public EmailSender(
        IOptions<EmailOptions> options,
        ILogger<EmailSender> logger,
        SipitexDbContext? db = null)
    {
        _options = options.Value; // .Value saca el objeto de IOptions
        _logger = logger;
        _db = db;
    }

    // Reviso si hay servidor SMTP listo: host, remitente y usuario.
    // Enabled queda como interruptor; sin usuario SMTP se usa outbox (desarrollo / demo).
    public bool IsSmtpConfigured =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.Host) &&
        !string.IsNullOrWhiteSpace(_options.From) &&
        !string.IsNullOrWhiteSpace(_options.User);

    // Manda el correo por SMTP o lo guarda en la base
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

        if (_db is null)
            throw new InvalidOperationException("No hay base de datos para guardar el correo en el outbox.");

        // INSERT directo: no arrastra otros cambios pendientes del mismo DbContext.
        // El cuerpo (códigos de confirmación) no se escribe en el log.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "EmailOutboxMessages" ("ToEmail", "ToName", "Subject", "Body", "CreatedAtUtc")
            VALUES ({toEmail}, {toName}, {subject}, {body}, {DateTime.UtcNow})
            """,
            cancellationToken);
        _logger.LogInformation("Correo guardado en la base (outbox) para {Email}: {Subject}", toEmail, subject);
    }
}
