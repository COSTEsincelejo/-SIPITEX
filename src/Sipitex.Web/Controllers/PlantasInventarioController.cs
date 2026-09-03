using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Catálogo de plantas de inventario: listar, crear, editar y borrar (solo Administrador)
[Authorize(Roles = UserRoles.Administrador)]
public class PlantasInventarioController : Controller
{
    private readonly IPlantaInventarioService _plantas;
    private readonly IActivityLogService _activityLog;

    public PlantasInventarioController(IPlantaInventarioService plantas, IActivityLogService activityLog)
    {
        _plantas = plantas;
        _activityLog = activityLog;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new PlantasInventarioIndexViewModel
        {
            PlantasInventario = await _plantas.GetAllAsync(cancellationToken),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Form")] CreatePlantaInventarioForm form,
        CancellationToken cancellationToken)
    {
        var result = await _plantas.CreateAsync(form.Nombre, cancellationToken);
        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                "CreatePlantaInventario",
                "PlantaInventario",
                entityId: form.Nombre?.Trim(),
                details: $"Nombre={form.Nombre?.Trim()}",
                cancellationToken);
        }

        TempData["Message"] = result.Message ?? (result.Success ? "Planta de inventario creada." : "No se pudo crear la planta de inventario.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var bodega = await _plantas.GetByIdAsync(id, cancellationToken);
        if (bodega is null)
            return NotFound();

        return View(new EditPlantaInventarioViewModel
        {
            Id = bodega.Id,
            Nombre = bodega.Nombre,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditPlantaInventarioViewModel model, CancellationToken cancellationToken)
    {
        var result = await _plantas.UpdateAsync(id, model.Nombre, cancellationToken);
        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                "UpdatePlantaInventario",
                "PlantaInventario",
                entityId: id.ToString(),
                details: $"Nombre={model.Nombre?.Trim()}",
                cancellationToken);
            TempData["Message"] = result.Message ?? "Planta de inventario actualizada.";
            TempData["IsSuccess"] = true;
            return RedirectToAction(nameof(Index));
        }

        model.Id = id;
        model.Message = result.Message ?? "No se pudo actualizar la planta de inventario.";
        model.IsSuccess = false;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _plantas.DeleteAsync(id, cancellationToken);
        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                "DeletePlantaInventario",
                "PlantaInventario",
                entityId: id.ToString(),
                details: result.Message,
                cancellationToken);
        }

        TempData["Message"] = result.Message ?? (result.Success ? "Planta de inventario eliminada." : "No se pudo eliminar la planta de inventario.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }
}
