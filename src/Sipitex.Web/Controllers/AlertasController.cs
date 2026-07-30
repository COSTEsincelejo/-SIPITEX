using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Preferencias de alertas por correo y el botón de evaluar manualmente
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

    // Pantalla principal: preferencias, historial y si SMTP está listo
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Id del usuario logueado para sus preferencias
        var userId = GetUserId();
        return View(new AlertasIndexViewModel
        {
            // Qué tipos de alerta tiene activos
            Preferences = await _alertService.GetPreferencesForUserAsync(userId, cancellationToken),
            // Últimos correos enviados (máx 20)
            Deliveries = await _alertService.GetRecentDeliveriesAsync(20, cancellationToken),
            // Si no hay SMTP configurado, la vista avisa
            SmtpConfigured = _emailSender.IsSmtpConfigured, // para avisar si el correo ni está configurado
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    // Guarda qué tipos de alerta quiere recibir el usuario por correo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreferences(AlertPreferencesForm form, CancellationToken cancellationToken)
    {
        // El checkbox manda strings; acá lo paso a diccionario AlertType -> bool
        var map = new Dictionary<AlertType, bool>();
        // Recorro todos los tipos posibles de alerta
        foreach (AlertType type in Enum.GetValues<AlertType>())
            // true si el usuario marcó ese checkbox
            map[type] = form.EnabledTypes?.Contains(type.ToString()) == true;

        // Persisto las preferencias del usuario actual
        await _alertService.SavePreferencesAsync(GetUserId(), map, cancellationToken);
        TempData["Message"] = "Preferencias de alerta guardadas.";
        TempData["IsSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }

    // Solo quien tenga el permiso puede forzar la evaluación (no esperar al job automático)
    [Authorize(Policy = AuthorizationPolicyNames.PuedeConfigurarAlertas)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluar(CancellationToken cancellationToken)
    {
        // El servicio revisa stock bajo, órdenes atrasadas, etc. y manda correos
        var result = await _alertService.EvaluateAndSendAsync(cancellationToken);
        // Resumen corto para TempData (máximo 5 detalles)
        TempData["Message"] = $"Alertas evaluadas: {result.AlertsFound} evento(s), {result.EmailsSent} correo(s). " +
                              string.Join(" | ", result.Details.Take(5));
        TempData["IsSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }

    // Saca el id del usuario de la cookie; si no hay, reviento porque no debería pasar
    private int GetUserId()
    {
        // Claim con el id numérico del usuario
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Si no parsea, es un error de sesión
        return int.TryParse(raw, out var id) ? id : throw new InvalidOperationException("Usuario no autenticado.");
    }
}
