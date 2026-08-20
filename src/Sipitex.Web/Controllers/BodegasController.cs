using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Catálogo de bodegas: listar y crear (solo Administrador)
[Authorize(Roles = UserRoles.Administrador)]
public class BodegasController : Controller
{
    private readonly IBodegaService _bodegas;
    private readonly IActivityLogService _activityLog;

    public BodegasController(IBodegaService bodegas, IActivityLogService activityLog)
    {
        _bodegas = bodegas;
        _activityLog = activityLog;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new BodegasIndexViewModel
        {
            Bodegas = await _bodegas.GetAllAsync(cancellationToken),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Form")] CreateBodegaForm form,
        CancellationToken cancellationToken)
    {
        var result = await _bodegas.CreateAsync(form.Nombre, cancellationToken);
        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                "CreateBodega",
                "Bodega",
                entityId: form.Nombre?.Trim(),
                details: $"Nombre={form.Nombre?.Trim()}",
                cancellationToken);
        }

        TempData["Message"] = result.Message ?? (result.Success ? "Bodega creada." : "No se pudo crear la bodega.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }
}
