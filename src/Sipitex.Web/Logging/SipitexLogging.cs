using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

namespace Sipitex.Web.Logging;

public static class SipitexLogging
{
    public const string ClaveArchivoPrueba = "Pruebas:LogArchivo";

    public static void Configurar(
        LoggerConfiguration logger,
        IConfiguration configuration,
        IHostEnvironment environment,
        IServiceProvider services)
    {
        logger
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();

        AplicarNiveles(logger, configuration);

        if (environment.IsProduction())
            logger.WriteTo.Console(new CompactJsonFormatter());
        else
            logger.WriteTo.Console();

        var archivo = configuration[ClaveArchivoPrueba];
        if (!string.IsNullOrWhiteSpace(archivo))
        {
            ITextFormatter formatter = environment.IsProduction()
                ? new CompactJsonFormatter()
                : new MessageTemplateTextFormatter("{Message:lj} {Properties:j}{NewLine}{Exception}");
            logger.WriteTo.Sink(new ArchivoPruebaSink(archivo, formatter));
        }
    }

    public static void AplicarNiveles(LoggerConfiguration logger, IConfiguration configuration)
    {
        var niveles = configuration.GetSection("Logging:LogLevel");
        logger.MinimumLevel.Is(Parse(niveles["Default"], LogEventLevel.Information));
        foreach (var child in niveles.GetChildren())
        {
            if (child.Key.Equals("Default", StringComparison.OrdinalIgnoreCase))
                continue;
            if (TryParse(child.Value, out var level))
                logger.MinimumLevel.Override(child.Key, level);
        }

        var serilogDefault = configuration["Serilog:MinimumLevel:Default"];
        if (TryParse(serilogDefault, out var serilogLevel))
            logger.MinimumLevel.Is(serilogLevel);

        foreach (var child in configuration.GetSection("Serilog:MinimumLevel:Override").GetChildren())
        {
            if (TryParse(child.Value, out var level))
                logger.MinimumLevel.Override(child.Key, level);
        }
    }

    public static void ConfigurarPeticion(Serilog.AspNetCore.RequestLoggingOptions options)
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0} ms";
        options.IncludeQueryInRequestPath = false;
        options.GetLevel = (http, _, ex) =>
        {
            if (ExcluirPeticion(http))
                return LogEventLevel.Verbose;
            if (ex is not null || http.Response.StatusCode >= 500)
                return LogEventLevel.Error;
            return LogEventLevel.Information;
        };
    }

    public static bool ExcluirPeticion(HttpContext http)
    {
        var path = http.Request.Path.Value ?? string.Empty;
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/healthz", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health/", StringComparison.OrdinalIgnoreCase))
            return true;

        var extension = Path.GetExtension(path);
        return extension is ".css" or ".js" or ".map" or ".png" or ".jpg" or ".jpeg"
            or ".gif" or ".svg" or ".ico" or ".woff" or ".woff2" or ".webp" or ".txt";
    }

    private static LogEventLevel Parse(string? value, LogEventLevel fallback) =>
        TryParse(value, out var level) ? level : fallback;

    private static bool TryParse(string? value, out LogEventLevel level)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            level = LogEventLevel.Information;
            return false;
        }

        if (value.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        {
            level = LogEventLevel.Fatal;
            return true;
        }

        if (value.Equals("Trace", StringComparison.OrdinalIgnoreCase))
        {
            level = LogEventLevel.Verbose;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out level);
    }
}

internal sealed class ArchivoPruebaSink : ILogEventSink
{
    private readonly string _path;
    private readonly ITextFormatter _formatter;
    private readonly Lock _lock = new();

    public ArchivoPruebaSink(string path, ITextFormatter formatter)
    {
        _path = path;
        _formatter = formatter;
    }

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        lock (_lock)
            File.AppendAllText(_path, writer.ToString());
    }
}
