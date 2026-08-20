using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Consulta de ActivityLog: quién hizo qué en acciones críticas (solo Administrador)
[Authorize(Roles = UserRoles.Administrador)]
public class AuditoriaController : Controller
{
    private readonly IActivityLogService _activityLog;

    public AuditoriaController(IActivityLogService activityLog) => _activityLog = activityLog;

    [HttpGet]
    public async Task<IActionResult> Index(
        DateOnly? desde,
        DateOnly? hasta,
        string? accion,
        string? entity,
        int? userId,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Auditoría";
        ViewData["Breadcrumb"] = "SIPITEX / Análisis / Auditoría";

        var entries = await _activityLog.QueryAsync(
            desde, hasta, accion, entity, userId, cancellationToken);

        return View(new AuditoriaIndexViewModel
        {
            Entries = entries,
            Actions = await _activityLog.GetDistinctActionsAsync(cancellationToken),
            Entities = await _activityLog.GetDistinctEntitiesAsync(cancellationToken),
            Actors = await _activityLog.GetDistinctActorsAsync(cancellationToken),
            Desde = desde,
            Hasta = hasta,
            Action = accion,
            Entity = entity,
            UserId = userId
        });
    }
}
