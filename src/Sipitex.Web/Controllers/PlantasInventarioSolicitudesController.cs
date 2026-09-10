using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Resolución de SolicitudMaterial por EncargadoDeBodega (PorFicha + InsumosLibres)
[Authorize(Roles = UserRoles.EncargadoDeBodega)]
public class PlantasInventarioSolicitudesController : Controller
{
    internal const string PlantaInventarioNoAsignadaMessage =
        "Su usuario de planta de inventario no tiene ninguna plantaInventario asignada. Pida al administrador que le asigne al menos una para ver y resolver solicitudes.";

    private readonly ISolicitudMaterialService _solicitudService;
    private readonly ISolicitudMaterialApprovalService _approvalService;
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentPlantaInventarioAccessor _plantaAccessor;

    public PlantasInventarioSolicitudesController(
        ISolicitudMaterialService solicitudService,
        ISolicitudMaterialApprovalService approvalService,
        IInventoryService inventoryService,
        ICurrentPlantaInventarioAccessor plantaAccessor)
    {
        _solicitudService = solicitudService;
        _approvalService = approvalService;
        _inventoryService = inventoryService;
        _plantaAccessor = plantaAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? estado, CancellationToken cancellationToken)
    {
        var soloPendientes = !string.Equals(estado, "todas", StringComparison.OrdinalIgnoreCase);
        var viewerPlantaInventarioIds = GetViewerPlantaInventarioIds();
        if (viewerPlantaInventarioIds is null)
        {
            return View(new PlantasInventarioSolicitudesIndexViewModel
            {
                Solicitudes = [],
                SoloPendientes = soloPendientes,
                Message = PlantaInventarioNoAsignadaMessage,
                IsSuccess = false
            });
        }

        var list = await _solicitudService.GetListForPlantaInventarioAsync(viewerPlantaInventarioIds, soloPendientes, cancellationToken);

        return View(new PlantasInventarioSolicitudesIndexViewModel
        {
            Solicitudes = list,
            SoloPendientes = soloPendientes,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var viewerPlantaInventarioIds = GetViewerPlantaInventarioIds();
        if (viewerPlantaInventarioIds is null)
        {
            TempData["Message"] = PlantaInventarioNoAsignadaMessage;
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var detail = await _solicitudService.GetResolucionDetailAsync(id, viewerPlantaInventarioIds, cancellationToken);
        if (detail is null)
            return NotFound();

        return View(new PlantaInventarioSolicitudDetailViewModel
        {
            Solicitud = detail,
            Materials = await _inventoryService.GetMaterialsAsync(cancellationToken),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(
        [Bind(Prefix = "Resolve")] ResolveSolicitudForm form,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var encargadoDeBodegaId))
        {
            TempData["Message"] = "Debe iniciar sesión como encargado de bodega.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var viewerPlantaInventarioIds = GetViewerPlantaInventarioIds();
        if (viewerPlantaInventarioIds is null)
        {
            TempData["Message"] = PlantaInventarioNoAsignadaMessage;
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var scoped = await _solicitudService.GetResolucionDetailAsync(
            form.SolicitudId, viewerPlantaInventarioIds, cancellationToken);
        if (scoped is null)
        {
            TempData["Message"] = "La solicitud no pertenece a su plantaInventario.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var items = (form.Items ?? [])
            .Select(i => new ResolveDetalleDto(
                i.DetalleId,
                i.CantidadAprobada,
                i.MaterialId is > 0 ? i.MaterialId : null,
                string.IsNullOrWhiteSpace(i.NewMaterialName) ? null : i.NewMaterialName.Trim(),
                i.NewMaterialUnit))
            .ToList();

        var result = await _approvalService.ResolveSolicitudAsync(
            form.SolicitudId,
            items,
            encargadoDeBodegaId,
            form.Observaciones,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud resuelta." : "No se pudo resolver.");
        TempData["IsSuccess"] = result.Success;

        if (result.Success)
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Detail), new { id = form.SolicitudId });
    }

    // EncargadoDeBodega sin asignaciones o sesión no restringida: no se listan todas las plantasInventario.
    private IReadOnlyList<int>? GetViewerPlantaInventarioIds()
    {
        var ids = _plantaAccessor.PlantaInventarioIds;
        if (ids is null || ids.Count == 0)
            return null;
        return ids;
    }
}
