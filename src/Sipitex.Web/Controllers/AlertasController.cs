using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class AlertasController : Controller
{
    private readonly IAlertService _alertService;
    private readonly IEmailSender _emailSender;

    public AlertasController(IAlertService alertService, IEmailSender emailSender)
    {
        _alertService = alertService;
        _emailSender = emailSender;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return View(new AlertasIndexViewModel
        {
            Preferences = await _alertService.GetPreferencesForUserAsync(userId, cancellationToken),
            Deliveries = await _alertService.GetRecentDeliveriesAsync(20, cancellationToken),
            SmtpConfigured = _emailSender.IsSmtpConfigured,
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

    [Authorize(Roles = UserRoles.Administrador)]
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

    private int GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : throw new InvalidOperationException("Usuario no autenticado.");
    }
}
