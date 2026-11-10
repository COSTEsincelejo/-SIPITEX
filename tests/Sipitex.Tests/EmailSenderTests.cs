using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Infrastructure.Email;

namespace Sipitex.Tests;

public class EmailSenderTests
{
    [Fact]
    public void IsSmtpConfigured_RequiereEnabledHostFromYUser()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = true,
            Host = "smtp.gmail.com",
            From = "sipitex@local",
            User = "user@smtp"
        }), NullLogger<EmailSender>.Instance);

        Assert.True(sender.IsSmtpConfigured);
    }

    [Fact]
    public void IsSmtpConfigured_SinUser_UsaOutbox()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = true,
            Host = "smtp.gmail.com",
            From = "sipitex@local",
            User = ""
        }), NullLogger<EmailSender>.Instance);

        Assert.False(sender.IsSmtpConfigured);
    }

    [Fact]
    public void IsSmtpConfigured_Disabled_FalsoAunqueHayaUser()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = false,
            Host = "smtp.gmail.com",
            From = "sipitex@local",
            User = "user@smtp"
        }), NullLogger<EmailSender>.Instance);

        Assert.False(sender.IsSmtpConfigured);
    }

    [Fact]
    public void DeliveryChannel_PorDefecto_EsResend()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions()), NullLogger<EmailSender>.Instance);

        Assert.Equal("Resend", sender.DeliveryChannel);
    }

    [Fact]
    public void DeliveryChannel_Smtp_SigueSeleccionable()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = true,
            Provider = "Smtp",
            Host = "smtp.gmail.com",
            From = "sipitex@local",
            User = "user@smtp"
        }), NullLogger<EmailSender>.Instance);

        Assert.Equal("Smtp", sender.DeliveryChannel);
        Assert.True(sender.IsSmtpConfigured);
    }

    [Fact]
    public async Task SendAsync_ResendSinCredenciales_FallaClaro_YNoRegistraElCodigoNiLaClave()
    {
        var logger = new ListLogger();
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = true,
            Provider = "Resend",
            ApiKey = "re_super_secret",
            FromAddress = ""
        }), logger);

        var ex = await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            sender.SendAsync("ana@gmail.com", "Ana", "Código de confirmación SIPITEX", "cuerpo\n123456\n"));

        Assert.Contains("Email__FromAddress", ex.Message);
        Assert.DoesNotContain("re_super_secret", ex.Message);
        Assert.DoesNotContain("123456", ex.Message);
        Assert.Contains(logger.Lines, line => line.Contains("a***@gmail.com", StringComparison.Ordinal) && line.Contains("Resend", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Lines, line => line.Contains("re_super_secret", StringComparison.Ordinal) || line.Contains("123456", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_EnabledFalse_NoSeDaPorEnviado()
    {
        var sender = new EmailSender(Options.Create(new EmailOptions
        {
            Enabled = false,
            Provider = "Resend",
            ApiKey = "re_super_secret",
            FromAddress = "avisos@sipitex.test"
        }), NullLogger<EmailSender>.Instance);

        var ex = await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            sender.SendAsync("ana@gmail.com", "Ana", "Asunto", "cuerpo"));

        Assert.Contains("Email:Enabled=false", ex.Message);
    }

    [Fact]
    public async Task SendAsync_Resend_PostHttps443_YEnmascaraElDestinatario()
    {
        var handler = new StubHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
        };
        var logger = new ListLogger();
        var sender = new EmailSender(
            Options.Create(new EmailOptions
            {
                Enabled = true,
                Provider = "Resend",
                ApiKey = "re_super_secret",
                FromAddress = "avisos@sipitex.test",
                FromName = "SIPITEX"
            }),
            logger,
            httpClientFactory: new StubFactory(handler));

        await sender.SendAsync("ana@gmail.com", "Ana", "Código de confirmación SIPITEX", "<p>123456</p>");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https", handler.LastRequest!.RequestUri!.Scheme);
        Assert.Equal(443, handler.LastRequest.RequestUri.Port);
        Assert.Equal("api.resend.com", handler.LastRequest.RequestUri.Host);
        Assert.Equal("/emails", handler.LastRequest.RequestUri.AbsolutePath);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("re_super_secret", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Contains("123456", handler.LastBody);
        Assert.Contains(logger.Lines, line =>
            line.Contains("a***@gmail.com", StringComparison.Ordinal)
            && line.Contains("Resultado=ok", StringComparison.Ordinal)
            && line.Contains("HttpStatus=200", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Lines, line =>
            line.Contains("re_super_secret", StringComparison.Ordinal) || line.Contains("123456", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_ResendHttpError_RegistraElEstado_SinElCuerpo()
    {
        var handler = new StubHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("{\"message\":\"domain\"}")
            }
        };
        var logger = new ListLogger();
        var sender = new EmailSender(
            Options.Create(new EmailOptions
            {
                Enabled = true,
                Provider = "Brevo",
                ApiKey = "xkeysib-secret",
                FromAddress = "avisos@sipitex.test",
                FromName = "SIPITEX"
            }),
            logger,
            httpClientFactory: new StubFactory(handler));

        var ex = await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            sender.SendAsync("ana@gmail.com", "Ana", "Asunto", "codigo 654321"));

        Assert.Equal(403, ex.StatusCode);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains(logger.Lines, line => line.Contains("HttpStatus=403", StringComparison.Ordinal) && line.Contains("Resultado=error", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Lines, line => line.Contains("xkeysib-secret", StringComparison.Ordinal) || line.Contains("654321", StringComparison.Ordinal));
        Assert.DoesNotContain("654321", ex.Message);
    }

    [Fact]
    public void StartupStatus_NoIncluyeLaApiKey()
    {
        var (_, message) = EmailConfiguration.StartupStatus(new EmailOptions
        {
            Enabled = true,
            Provider = "Resend",
            ApiKey = "re_super_secret",
            FromAddress = ""
        });

        Assert.Contains("Email__ApiKey", message);
        Assert.DoesNotContain("re_super_secret", message);
    }

    [Fact]
    public async Task SendAsync_Brevo201_EnviaSenderRecortado_YRegistraMessageId()
    {
        var handler = new StubHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"messageId\":\"brevo-msg-201\"}")
            }
        };
        var logger = new ListLogger();
        var sender = new EmailSender(
            Options.Create(new EmailOptions
            {
                Enabled = true,
                Provider = "Brevo",
                ApiKey = "  xkeysib-secret\r\n",
                FromAddress = "  SIPITEX <avisos@sipitex.test>  ",
                FromName = "  SIPITEX  "
            }),
            logger,
            httpClientFactory: new StubFactory(handler));

        await sender.SendAsync(
            "ana@gmail.com",
            "Ana",
            "Código de confirmación SIPITEX",
            "<p>123456</p>");

        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(443, handler.LastRequest.RequestUri.Port);
        Assert.True(handler.LastRequest.Headers.TryGetValues("api-key", out var keys));
        Assert.Equal("xkeysib-secret", Assert.Single(keys));

        using var doc = JsonDocument.Parse(handler.LastBody!);
        var root = doc.RootElement;
        Assert.Equal("avisos@sipitex.test", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("SIPITEX", root.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("ana@gmail.com", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Código de confirmación SIPITEX", root.GetProperty("subject").GetString());
        Assert.Contains("123456", root.GetProperty("htmlContent").GetString());
        Assert.False(root.TryGetProperty("html", out _));

        Assert.Contains(logger.Lines, line =>
            line.Contains("Resultado=ok", StringComparison.Ordinal)
            && line.Contains("HttpStatus=201", StringComparison.Ordinal)
            && line.Contains("MessageId=brevo-msg-201", StringComparison.Ordinal)
            && line.Contains("a***@gmail.com", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Lines, line =>
            line.Contains("xkeysib-secret", StringComparison.Ordinal)
            || line.Contains("123456", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_Brevo400_LanzaYRegistraElCuerpoSinSecretos()
    {
        var handler = new StubHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    "{\"code\":\"invalid_parameter\",\"message\":\"email is not valid in sender. echo 123456 xkeysib-secret\"}")
            }
        };
        var logger = new ListLogger();
        var sender = BrevoSender(handler, logger);

        var ex = await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            sender.SendAsync("ana@gmail.com", "Ana", "Asunto", "<p>123456</p>"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("email is not valid in sender", ex.ProviderDetail);
        Assert.DoesNotContain("123456", ex.ProviderDetail);
        Assert.DoesNotContain("xkeysib-secret", ex.ProviderDetail);
        Assert.Contains(logger.Lines, line =>
            line.Contains("Resultado=error", StringComparison.Ordinal)
            && line.Contains("HttpStatus=400", StringComparison.Ordinal)
            && line.Contains("email is not valid in sender", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Lines, line =>
            line.Contains("xkeysib-secret", StringComparison.Ordinal)
            || line.Contains("123456", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_Brevo401_LanzaYRegistraElCuerpoSinLaClave()
    {
        var handler = new StubHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"code\":\"unauthorized\",\"message\":\"Key not found\"}")
            }
        };
        var logger = new ListLogger();
        var sender = BrevoSender(handler, logger);

        var ex = await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            sender.SendAsync("ana@gmail.com", "Ana", "Asunto", "<p>654321</p>"));

        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("Key not found", ex.ProviderDetail);
        Assert.Contains(logger.Lines, line =>
            line.Contains("HttpStatus=401", StringComparison.Ordinal)
            && line.Contains("Key not found", StringComparison.Ordinal)
            && line.Contains("ErrorBody=", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Lines, line =>
            line.Contains("xkeysib-secret", StringComparison.Ordinal)
            || line.Contains("654321", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_Resend_AdjuntaElPdfEnBase64()
    {
        var handler = new StubHandler { Response = new HttpResponseMessage(HttpStatusCode.OK) };
        var sender = new EmailSender(
            Options.Create(new EmailOptions
            {
                Enabled = true,
                Provider = "Resend",
                ApiKey = "re_super_secret",
                FromAddress = "avisos@sipitex.test",
                FromName = "SIPITEX"
            }),
            NullLogger<EmailSender>.Instance,
            httpClientFactory: new StubFactory(handler));
        var bytes = new byte[] { 1, 2, 3, 4 };

        await sender.SendAsync(
            "ana@gmail.com",
            "Ana",
            "Acta",
            "cuerpo",
            [new EmailAttachment("ACT-0001.pdf", "application/pdf", bytes)]);

        using var doc = JsonDocument.Parse(handler.LastBody!);
        var adjunto = doc.RootElement.GetProperty("attachments")[0];
        Assert.Equal("ACT-0001.pdf", adjunto.GetProperty("filename").GetString());
        Assert.Equal(Convert.ToBase64String(bytes), adjunto.GetProperty("content").GetString());
    }

    [Fact]
    public async Task SendAsync_Brevo_AdjuntaElPdfEnBase64()
    {
        var handler = new StubHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"messageId\":\"m1\"}")
            }
        };
        var sender = BrevoSender(handler, new ListLogger());
        var bytes = new byte[] { 9, 8, 7 };

        await sender.SendAsync(
            "ana@gmail.com",
            "Ana",
            "Acta",
            "cuerpo",
            [new EmailAttachment("ACT-0002.pdf", "application/pdf", bytes)]);

        using var doc = JsonDocument.Parse(handler.LastBody!);
        var adjunto = doc.RootElement.GetProperty("attachment")[0];
        Assert.Equal("ACT-0002.pdf", adjunto.GetProperty("name").GetString());
        Assert.Equal(Convert.ToBase64String(bytes), adjunto.GetProperty("content").GetString());
    }

    [Fact]
    public async Task SendAsync_AdjuntoMayorA5Mb_NoSale()
    {
        var handler = new StubHandler();
        var sender = new EmailSender(
            Options.Create(new EmailOptions
            {
                Enabled = true,
                Provider = "Resend",
                ApiKey = "re_super_secret",
                FromAddress = "avisos@sipitex.test"
            }),
            NullLogger<EmailSender>.Instance,
            httpClientFactory: new StubFactory(handler));

        var ex = await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            sender.SendAsync(
                "ana@gmail.com",
                "Ana",
                "Acta",
                "cuerpo",
                [new EmailAttachment("grande.pdf", "application/pdf", new byte[EmailSender.MaxAttachmentBytes + 1])]));

        Assert.Contains("5 MB", ex.Message, StringComparison.Ordinal);
        Assert.Null(handler.LastRequest);
    }

    private static EmailSender BrevoSender(StubHandler handler, ListLogger logger) =>
        new(
            Options.Create(new EmailOptions
            {
                Enabled = true,
                Provider = "Brevo",
                ApiKey = "xkeysib-secret",
                FromAddress = "avisos@sipitex.test",
                FromName = "SIPITEX"
            }),
            logger,
            httpClientFactory: new StubFactory(handler));

    private sealed class ListLogger : ILogger<EmailSender>
    {
        public List<string> Lines { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Lines.Add(formatter(state, exception) + " " + exception);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose()
        {
        }
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubFactory(HttpMessageHandler handler) => _client = new HttpClient(handler);

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return Response;
        }
    }
}
