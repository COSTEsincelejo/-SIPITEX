using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
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
                    LogAttempt(toEmail, display, "outbox", null, null, null);
                    return;
                case EmailConfiguration.Resend:
                    var resend = await SendResendAsync(toEmail, toName, subject, body, cancellationToken);
                    status = resend.Status;
                    LogAttempt(toEmail, display, "ok", status, null, resend.MessageId);
                    return;
                case EmailConfiguration.Brevo:
                    var brevo = await SendBrevoAsync(toEmail, toName, subject, body, cancellationToken);
                    status = brevo.Status;
                    LogAttempt(toEmail, display, "ok", status, null, brevo.MessageId);
                    return;
                case EmailConfiguration.Smtp:
                    await SendSmtpAsync(toEmail, toName, subject, body, cancellationToken);
                    break;
                default:
                    throw new EmailDeliveryException(
                        $"Proveedor de correo no reconocido ({_options.Provider}). Use Resend, Brevo, Smtp u Outbox.");
            }

            LogAttempt(toEmail, display, "ok", status, null, null);
        }
        catch (EmailDeliveryException ex)
        {
            LogAttempt(toEmail, display, "error", ex.StatusCode ?? status, ex, ex.ProviderDetail);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogAttempt(toEmail, display, "error", status, ex, null);
            throw new EmailDeliveryException("No se pudo enviar el correo.", status, ex);
        }
    }

    private async Task<ProviderCall> SendResendAsync(
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

        var apiKey = (_options.ApiKey ?? string.Empty).Trim();
        return await PostJsonAsync(
            "https://api.resend.com/emails",
            payload,
            request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey),
            "Resend",
            apiKey,
            cancellationToken);
    }

    private async Task<ProviderCall> SendBrevoAsync(
        string toEmail,
        string toName,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        EnsureApiReady(EmailConfiguration.Brevo);
        var apiKey = (_options.ApiKey ?? string.Empty).Trim();
        var senderEmail = EmailConfiguration.PlainAddress(EmailConfiguration.ResolveFromAddress(_options)).Trim();
        var senderName = string.IsNullOrWhiteSpace(_options.FromName) ? "SIPITEX" : _options.FromName.Trim();
        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new Dictionary<string, string>
            {
                ["name"] = senderName,
                ["email"] = senderEmail
            },
            ["to"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["email"] = toEmail.Trim(),
                    ["name"] = string.IsNullOrWhiteSpace(toName) ? toEmail.Trim() : toName.Trim()
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
            request =>
            {
                if (!request.Headers.TryAddWithoutValidation("api-key", apiKey))
                {
                    throw new EmailDeliveryException(
                        "La clave de Brevo no pudo ir en el header api-key. Revise Email__ApiKey.");
                }
            },
            "Brevo",
            apiKey,
            cancellationToken);
    }

    private readonly record struct ProviderCall(int Status, string? MessageId);

    private async Task<ProviderCall> PostJsonAsync(
        string url,
        object payload,
        Action<HttpRequestMessage> configure,
        string providerName,
        string secretToRedact,
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
            var rawBody = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = SanitizeProviderText(rawBody, secretToRedact);
                throw new EmailDeliveryException(
                    $"El proveedor {providerName} respondió HTTP {status}.",
                    status)
                {
                    ProviderDetail = errorBody
                };
            }

            return new ProviderCall(status, ExtractMessageId(rawBody, secretToRedact));
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

    private void LogAttempt(
        string toEmail,
        string provider,
        string result,
        int? status,
        Exception? exception,
        string? detail)
    {
        var recipient = EmailAddressMask.Mask(toEmail);
        if (exception is null && string.Equals(result, "ok", StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Envío de correo. Destinatario={Recipient} Proveedor={Provider} Resultado={Result} HttpStatus={Status} MessageId={MessageId}",
                recipient,
                provider,
                result,
                status,
                detail ?? string.Empty);
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
            "Envío de correo. Destinatario={Recipient} Proveedor={Provider} Resultado={Result} HttpStatus={Status} ErrorBody={ErrorBody}",
            recipient,
            provider,
            result,
            status,
            detail ?? string.Empty);
    }

    // Quita la API key y cualquier código de 6 dígitos antes de escribir el log.
    private static string SanitizeProviderText(string? text, string? secret)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var clean = text.Trim();
        if (!string.IsNullOrWhiteSpace(secret))
            clean = clean.Replace(secret, "[redacted]", StringComparison.Ordinal);

        clean = SixDigitCode.Replace(clean, "[codigo]");
        if (clean.Length > 500)
            clean = clean[..500];

        return clean;
    }

    private static string? ExtractMessageId(string rawBody, string secret)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            foreach (var name in new[] { "messageId", "id" })
            {
                if (doc.RootElement.TryGetProperty(name, out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    var id = SanitizeProviderText(value.GetString(), secret);
                    return string.IsNullOrWhiteSpace(id) ? null : id;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static readonly Regex SixDigitCode = new(@"\b\d{6}\b", RegexOptions.Compiled);

    private static bool IsHtml(string body) =>
        body.Contains("<div", StringComparison.OrdinalIgnoreCase)
        || body.Contains("<p", StringComparison.OrdinalIgnoreCase)
        || body.Contains("<html", StringComparison.OrdinalIgnoreCase);
}
