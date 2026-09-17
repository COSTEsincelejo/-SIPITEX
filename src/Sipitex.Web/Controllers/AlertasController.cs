using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sipitex.Application.Authorization;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Email;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Preferencias de alertas por correo y el botón de evaluar manualmente
[Authorize]
public class AlertasController : Controller
{
    private readonly IAlertService _alertService;
    private readonly IStatisticsService _statisticsService;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;

    public AlertasController(
        IAlertService alertService,
        IStatisticsService statisticsService,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions)
    {
        _alertService = alertService;
        _statisticsService = statisticsService;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(UserRoles.Administrador);
        int? viewerUserId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0)
            viewerUserId = id;
        var dashboard = await _statisticsService.GetDashboardAsync(
            viewerUserId,
            User.FindFirstValue(ClaimTypes.Role),
            User.FindFirstValue(ClaimTypes.Name),
            cancellationToken);
        return View(new AlertasIndexViewModel
        {
            Preferences = await _alertService.GetPreferencesForUserAsync(userId, cancellationToken),
            Deliveries = await _alertService.GetRecentDeliveriesAsync(20, isAdmin ? null : userId, cancellationToken),
            OkStockCount = dashboard.OkStockCount,
            LowStockCount = dashboard.LowStockCount,
            CriticalStockCount = dashboard.CriticalStockCount,
            SmtpConfigured = _emailSender.IsSmtpConfigured,
            SmtpHost = _emailOptions.Host,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreferences(AlertPreferencesForm form, CancellationToken cancellationToken)
    {
        var map = new Dictionary<AlertType, bool>();
        foreach (AlertType type in Enum.GetValues<AlertType>())
            map[type] = form.EnabledTypes?.Contains(type.ToString()) == true;

        await _alertService.SavePreferencesAsync(GetUserId(), map, cancellationToken);
        TempData["Message"] = "Preferencias de alerta guardadas.";
        TempData["IsSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeConfigurarAlertas)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluar(CancellationToken cancellationToken)
    {
        var result = await _alertService.EvaluateAndSendAsync(cancellationToken);
        TempData["Message"] = $"Alertas evaluadas: {result.AlertsFound} evento(s), {result.EmailsSent} correo(s). " +
                              string.Join(" | ", result.Details.Take(5));
        TempData["IsSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Probar(CancellationToken cancellationToken)
    {
        var result = await _alertService.SendTestAsync(GetUserId(), cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    private int GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : throw new InvalidOperationException("Usuario no autenticado.");
    }
}
