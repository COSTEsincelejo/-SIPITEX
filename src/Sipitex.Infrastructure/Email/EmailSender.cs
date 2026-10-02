using System.Net.Http.Headers;
using System.Net.Http.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Email;

// Opciones del correo. La sección "Email" se llena desde appsettings y variables Email__*.
public class EmailOptions
{
    public const string SectionName = "Email";
    public bool Enabled { get; set; }
    // Resend (predeterminado), Brevo, Smtp u Outbox.
    public string Provider { get; set; } = EmailConfiguration.Resend;
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "SIPITEX";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    // Remitente SMTP heredado. El canal API usa FromAddress.
    public string From { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string OutboxPath { get; set; } = "email-outbox";
}

// Único punto de envío: API HTTPS (Resend o Brevo), SMTP o outbox explícito.
public class EmailSender : IEmailSender
{
    public const string HttpClientName = "sipitex-email";

    private readonly EmailOptions _options;
    private readonly ILogger<EmailSender> _logger;
    private readonly SipitexDbContext? _db;
    private readonly IHttpClientFactory? _httpClientFactory;

    public EmailSender(
        IOptions<EmailOptions> options,
        ILogger<EmailSender> logger,
        SipitexDbContext? db = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        _options = options.Value;
        _logger = logger;
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    // Se mantiene el criterio histórico: interruptor, host, remitente y usuario SMTP.
    public bool IsSmtpConfigured => EmailConfiguration.IsLegacySmtpReady(_options);

    public string DeliveryChannel => EmailConfiguration.DisplayName(EmailConfiguration.ResolveProvider(_options));

    public async Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken = default)
    {
        var provider = EmailConfiguration.ResolveProvider(_options);
        var display = EmailConfiguration.DisplayName(provider);
        int? status = null;

        try
        {
            switch (provider)
            {
                case EmailConfiguration.Outbox:
                    await SendOutboxAsync(toEmail, toName, subject, body, cancellationToken);
                    LogAttempt(toEmail, display, "outbox", null, null);
                    return;
                case EmailConfiguration.Resend:
                    status = await SendResendAsync(toEmail, toName, subject, body, cancellationToken);
                    break;
                case EmailConfiguration.Brevo:
                    status = await SendBrevoAsync(toEmail, toName, subject, body, cancellationToken);
                    break;
                case EmailConfiguration.Smtp:
                    await SendSmtpAsync(toEmail, toName, subject, body, cancellationToken);
                    break;
                default:
                    throw new EmailDeliveryException(
                        $"Proveedor de correo no reconocido ({_options.Provider}). Use Resend, Brevo, Smtp u Outbox.");
            }

            LogAttempt(toEmail, display, "ok", status, null);
        }
        catch (EmailDeliveryException ex)
        {
            LogAttempt(toEmail, display, "error", ex.StatusCode ?? status, ex);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogAttempt(toEmail, display, "error", status, ex);
            throw new EmailDeliveryException("No se pudo enviar el correo.", status, ex);
        }
    }

    private async Task<int> SendResendAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        EnsureApiReady(EmailConfiguration.Resend);
        var safeName = string.IsNullOrWhiteSpace(toName)
            ? string.Empty
            : toName.Replace("<", string.Empty, StringComparison.Ordinal).Replace(">", string.Empty, StringComparison.Ordinal);
        var recipient = string.IsNullOrWhiteSpace(safeName) ? toEmail : $"{safeName} <{toEmail}>";
        var payload = new Dictionary<string, object?>
        {
            ["from"] = EmailConfiguration.FormatResendFrom(_options),
            ["to"] = new[] { recipient },
            ["subject"] = subject
        };
        if (IsHtml(body))
            payload["html"] = body;
        else
            payload["text"] = body;

        return await PostJsonAsync(
            "https://api.resend.com/emails",
            payload,
            request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim()),
            "Resend",
            cancellationToken);
    }

    private async Task<int> SendBrevoAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        EnsureApiReady(EmailConfiguration.Brevo);
        var address = EmailConfiguration.PlainAddress(EmailConfiguration.ResolveFromAddress(_options));
        var senderName = string.IsNullOrWhiteSpace(_options.FromName) ? "SIPITEX" : _options.FromName.Trim();
        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new Dictionary<string, string>
            {
                ["name"] = senderName,
                ["email"] = address
            },
            ["to"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["email"] = toEmail,
                    ["name"] = string.IsNullOrWhiteSpace(toName) ? toEmail : toName
                }
            },
            ["subject"] = subject
        };
        if (IsHtml(body))
            payload["htmlContent"] = body;
        else
            payload["textContent"] = body;

        return await PostJsonAsync(
            "https://api.brevo.com/v3/smtp/email",
            payload,
            request => request.Headers.TryAddWithoutValidation("api-key", _options.ApiKey.Trim()),
            "Brevo",
            cancellationToken);
    }

    private async Task<int> PostJsonAsync(
        string url,
        object payload,
        Action<HttpRequestMessage> configure,
        string providerName,
        CancellationToken cancellationToken)
    {
        if (_httpClientFactory is null)
        {
            throw new EmailDeliveryException(
                $"No hay un cliente HTTP para {providerName}. Revise el registro de IHttpClientFactory.");
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        configure(request);
        request.Content = JsonContent.Create(payload);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new EmailDeliveryException(
                $"No se pudo contactar a {providerName} por HTTPS (puerto 443).",
                innerException: ex);
        }

        using (response)
        {
            var status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                throw new EmailDeliveryException(
                    $"El proveedor {providerName} respondió HTTP {status}.",
                    status);
            }

            return status;
        }
    }

    private void EnsureApiReady(string provider)
    {
        var problem = EmailConfiguration.DescribeBlockingProblem(_options);
        if (problem is not null)
            throw new EmailDeliveryException(problem);

        if (!string.Equals(EmailConfiguration.ResolveProvider(_options), provider, StringComparison.Ordinal))
            throw new EmailDeliveryException($"El proveedor activo no es {provider}.");
    }

    private async Task SendSmtpAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var problem = EmailConfiguration.DescribeBlockingProblem(_options);
        if (problem is not null)
            throw new EmailDeliveryException(problem);

        var fromAddress = EmailConfiguration.SmtpFrom(_options);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = IsHtml(body)
            ? new TextPart("html") { Text = body }
            : new TextPart("plain") { Text = body };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.User))
                await client.AuthenticateAsync(_options.User, _options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not EmailDeliveryException)
        {
            throw new EmailDeliveryException(
                "El servidor SMTP no aceptó el mensaje. En Render gratis use Email__Provider=Resend.",
                innerException: ex);
        }
    }

    private async Task SendOutboxAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (_db is null)
            throw new EmailDeliveryException("No hay base de datos para guardar el correo en el outbox.");

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "EmailOutboxMessages" ("ToEmail", "ToName", "Subject", "Body", "CreatedAtUtc")
            VALUES ({toEmail}, {toName}, {subject}, {body}, {DateTime.UtcNow})
            """,
            cancellationToken);
    }

    private void LogAttempt(string toEmail, string provider, string result, int? status, Exception? exception)
    {
        var recipient = EmailAddressMask.Mask(toEmail);
        if (exception is null && string.Equals(result, "ok", StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Envío de correo. Destinatario={Recipient} Proveedor={Provider} Resultado={Result} HttpStatus={Status}",
                recipient,
                provider,
                result,
                status);
            return;
        }

        if (exception is null)
        {
            _logger.LogWarning(
                "Envío de correo. Destinatario={Recipient} Proveedor={Provider} Resultado={Result} HttpStatus={Status}. No se entregó al buzón del destinatario.",
                recipient,
                provider,
                result,
                status);
            return;
        }

        _logger.LogError(
            exception,
            "Envío de correo. Destinatario={Recipient} Proveedor={Provider} Resultado={Result} HttpStatus={Status}",
            recipient,
            provider,
            result,
            status);
    }

    private static bool IsHtml(string body) =>
        body.Contains("<div", StringComparison.OrdinalIgnoreCase)
        || body.Contains("<p", StringComparison.OrdinalIgnoreCase)
        || body.Contains("<html", StringComparison.OrdinalIgnoreCase);
}
