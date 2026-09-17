using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Catálogo de plantasInventario (CRUD: solo Administrador) y consulta de inventario por planta.
[Authorize]
public class PlantasInventarioController : Controller
{
    private readonly IPlantaInventarioService _plantas;
    private readonly IPlantaInventarioReassignmentService _reassignment;
    private readonly IActivityLogService _activityLog;
    private readonly IInventoryService _inventory;
    private readonly ICurrentPlantaInventarioAccessor _plantaAccessor;

    public PlantasInventarioController(
        IPlantaInventarioService plantasInventario,
        IPlantaInventarioReassignmentService reassignment,
        IActivityLogService activityLog,
        IInventoryService inventory,
        ICurrentPlantaInventarioAccessor plantaAccessor)
    {
        _plantas = plantasInventario;
        _reassignment = reassignment;
        _activityLog = activityLog;
        _inventory = inventory;
        _plantaAccessor = plantaAccessor;
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new PlantasInventarioIndexViewModel
        {
            PlantasInventario = await _plantas.GetAllAsync(cancellationToken),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor},{UserRoles.EncargadoDeBodega}")]
    public async Task<IActionResult> Consultar(int? plantaInventarioId, CancellationToken cancellationToken = default)
    {
        var plantas = VisiblePlantas(await _plantas.GetAllAsync(cancellationToken));
        var materials = await _inventory.GetMaterialsByPlantaAsync(plantaInventarioId, cancellationToken);

        IReadOnlyList<PlantaInventarioResumenItem> resumen = [];
        if (plantaInventarioId is null)
        {
            resumen = plantas.Select(p =>
            {
                var mats = materials.Where(m => m.PlantaInventarioId == p.Id).ToList();
                return new PlantaInventarioResumenItem
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Materiales = mats.Count,
                    Bajo = mats.Count(m => StockNivelHelper.Classify(m.Stock, m.MinStock) == StockNivel.Bajo),
                    Critico = mats.Count(m => StockNivelHelper.Classify(m.Stock, m.MinStock) == StockNivel.Critico)
                };
            }).ToList();
        }

        return View(new ConsultarPlantasInventarioViewModel
        {
            PlantaInventarioId = plantaInventarioId,
            Plantas = plantas,
            Materials = materials,
            Resumen = resumen
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
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
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var plantaInventario = await _plantas.GetByIdAsync(id, cancellationToken);
        if (plantaInventario is null)
            return NotFound();

        return View(new EditPlantaInventarioViewModel
        {
            Id = plantaInventario.Id,
            Nombre = plantaInventario.Nombre,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
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
        model.Message = result.Message ?? "No se pudo actualizar la plantaInventario.";
        model.IsSuccess = false;
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var planta = await _plantas.GetByIdAsync(id, cancellationToken);
        if (planta is null)
            return NotFound();

        var deps = await _plantas.GetDependenciasAsync(id, cancellationToken);
        var destinos = (await _plantas.GetAllAsync(cancellationToken))
            .Where(p => p.Activo && p.Id != id)
            .ToList();

        return View(new DeletePlantaInventarioViewModel
        {
            Id = planta.Id,
            Nombre = planta.Nombre,
            Activo = planta.Activo,
            Materiales = deps.Materiales,
            Solicitudes = deps.Solicitudes,
            Encargados = deps.Encargados,
            StockTotal = deps.StockTotal,
            Destinos = destinos,
            DestinoId = destinos.FirstOrDefault()?.Id ?? 0,
            Message = TempData["Message"] as string
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Delete(int id, int? destinoId, CancellationToken cancellationToken)
    {
        var deps = await _plantas.GetDependenciasAsync(id, cancellationToken);
        ServiceResult result;
        string action = ActivityLogActions.DeletePlantaInventario;

        if (deps.Any)
        {
            if (destinoId is null or <= 0)
            {
                TempData["Message"] = "Seleccione una planta de inventario destino para reasignar el stock y las dependencias.";
                TempData["IsSuccess"] = false;
                return RedirectToAction(nameof(Delete), new { id });
            }

            result = await _reassignment.ReassignAndDeleteAsync(id, destinoId.Value, cancellationToken);
            action = ActivityLogActions.ReassignPlantaInventario;
        }
        else
        {
            result = await _plantas.DeleteAsync(id, cancellationToken);
        }

        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                action,
                "PlantaInventario",
                entityId: id.ToString(),
                details: result.Message,
                cancellationToken);
        }

        TempData["Message"] = result.Message ?? (result.Success ? "Planta de inventario actualizada." : "No se pudo eliminar la planta de inventario.");
        TempData["IsSuccess"] = result.Success;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Delete), new { id });
    }

    private IReadOnlyList<PlantaInventario> VisiblePlantas(IReadOnlyList<PlantaInventario> plantas)
    {
        var allowed = _plantaAccessor.PlantaInventarioIds;
        if (allowed is null)
            return plantas;

        return plantas.Where(p => allowed.Contains(p.Id)).ToList();
    }
}
