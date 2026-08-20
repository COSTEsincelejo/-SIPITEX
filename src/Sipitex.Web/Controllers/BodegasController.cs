using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Catálogo de bodegas: listar, crear, editar y borrar (solo Administrador)
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

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var bodega = await _bodegas.GetByIdAsync(id, cancellationToken);
        if (bodega is null)
            return NotFound();

        return View(new EditBodegaViewModel
        {
            Id = bodega.Id,
            Nombre = bodega.Nombre,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditBodegaViewModel model, CancellationToken cancellationToken)
    {
        var result = await _bodegas.UpdateAsync(id, model.Nombre, cancellationToken);
        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                "UpdateBodega",
                "Bodega",
                entityId: id.ToString(),
                details: $"Nombre={model.Nombre?.Trim()}",
                cancellationToken);
            TempData["Message"] = result.Message ?? "Bodega actualizada.";
            TempData["IsSuccess"] = true;
            return RedirectToAction(nameof(Index));
        }

        model.Id = id;
        model.Message = result.Message ?? "No se pudo actualizar la bodega.";
        model.IsSuccess = false;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _bodegas.DeleteAsync(id, cancellationToken);
        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                "DeleteBodega",
                "Bodega",
                entityId: id.ToString(),
                details: result.Message,
                cancellationToken);
        }

        TempData["Message"] = result.Message ?? (result.Success ? "Bodega eliminada." : "No se pudo eliminar la bodega.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }
}
