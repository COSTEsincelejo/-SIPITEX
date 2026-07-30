using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Web.Hosting;

// Servicio que corre en background mientras la web está levantada
public class AlertEvaluationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertEvaluationHostedService> _logger;

    public AlertEvaluationHostedService(IServiceScopeFactory scopeFactory, ILogger<AlertEvaluationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Espera inicial para no competir con el arranque/seed.
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Scope aparte porque el hosted service es singleton y los servicios son scoped
                using var scope = _scopeFactory.CreateScope();
                var alerts = scope.ServiceProvider.GetRequiredService<IAlertService>();
                var result = await alerts.EvaluateAndSendAsync(stoppingToken);
                _logger.LogInformation("Alertas programadas: {Found} eventos, {Sent} envíos", result.AlertsFound, result.EmailsSent);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al evaluar alertas programadas");
            }

            // Cada 6 horas vuelve a revisar
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
