using Microsoft.Extensions.Logging;

namespace Sipitex.Infrastructure.Email;

// Elige el canal y describe qué falta, sin repetir la API key.
public static class EmailConfiguration
{
    public const string Resend = "resend";
    public const string Brevo = "brevo";
    public const string Smtp = "smtp";
    public const string Outbox = "outbox";

    public static string ResolveProvider(EmailOptions options)
    {
        var raw = (options.Provider ?? string.Empty).Trim().ToLowerInvariant();
        if (raw is Resend or Brevo or Smtp or Outbox)
            return raw;

        if (!string.IsNullOrEmpty(raw))
            return raw;

        return Resend;
    }

    public static string DisplayName(string provider) => provider switch
    {
        Resend => "Resend",
        Brevo => "Brevo",
        Smtp => "Smtp",
        Outbox => "Outbox",
        _ => provider
    };

    public static string ResolveFromAddress(EmailOptions options) =>
        string.IsNullOrWhiteSpace(options.FromAddress) ? string.Empty : options.FromAddress.Trim();

    public static string PlainAddress(string value)
    {
        var start = value.IndexOf('<');
        var end = value.IndexOf('>');
        if (start >= 0 && end > start)
            return value[(start + 1)..end].Trim();

        return value.Trim();
    }

    public static string FormatResendFrom(EmailOptions options)
    {
        var address = ResolveFromAddress(options);
        if (address.Contains('<', StringComparison.Ordinal))
            return address;

        var name = string.IsNullOrWhiteSpace(options.FromName) ? "SIPITEX" : options.FromName.Trim();
        return $"{name} <{address}>";
    }

    public static bool IsLegacySmtpReady(EmailOptions options) =>
        options.Enabled
        && !string.IsNullOrWhiteSpace(options.Host)
        && !string.IsNullOrWhiteSpace(SmtpFrom(options))
        && !string.IsNullOrWhiteSpace(options.User);

    public static string SmtpFrom(EmailOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.FromAddress))
            return PlainAddress(options.FromAddress);
        return (options.From ?? string.Empty).Trim();
    }

    // null = el proveedor elegido puede entregar. El texto no incluye secretos.
    public static string? DescribeBlockingProblem(EmailOptions options)
    {
        var provider = ResolveProvider(options);
        if (provider == Outbox)
            return null;

        if (!options.Enabled)
        {
            return "Email:Enabled=false. El envío real está apagado y el mensaje no se guarda como si hubiera salido. " +
                   "Defina Email__Enabled=true, Email__Provider=Resend, Email__ApiKey, Email__FromAddress y Email__FromName.";
        }

        return provider switch
        {
            Resend when !HasApiCredentials(options) =>
                "Proveedor Resend incompleto. Defina Email__ApiKey y Email__FromAddress. " +
                "El plan gratis de Render bloquea SMTP (puertos 25, 465 y 587); el envío va por HTTPS.",
            Brevo when !HasApiCredentials(options) =>
                "Proveedor Brevo incompleto. Defina Email__ApiKey y Email__FromAddress.",
            Smtp when !IsLegacySmtpReady(options) =>
                "Proveedor Smtp incompleto. Defina Email__Host, Email__From o Email__FromAddress, Email__User y Email__Password.",
            Resend or Brevo or Smtp => null,
            _ => $"Proveedor de correo no reconocido ({options.Provider}). Use Resend, Brevo, Smtp u Outbox."
        };
    }

    public static (LogLevel Level, string Message) StartupStatus(EmailOptions options)
    {
        var provider = DisplayName(ResolveProvider(options));
        if (string.Equals(ResolveProvider(options), Outbox, StringComparison.Ordinal))
        {
            return (LogLevel.Warning,
                "Email:Provider=Outbox. Los correos se guardan en EmailOutboxMessages y no llegan al destinatario.");
        }

        var problem = DescribeBlockingProblem(options);
        if (problem is null)
            return (LogLevel.Information, $"Correo listo. Proveedor={provider}.");

        return (LogLevel.Error, problem);
    }

    private static bool HasApiCredentials(EmailOptions options) =>
        !string.IsNullOrWhiteSpace(options.ApiKey)
        && !string.IsNullOrWhiteSpace(ResolveFromAddress(options));
}
