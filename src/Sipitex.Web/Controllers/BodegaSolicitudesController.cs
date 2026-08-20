using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Resolución de SolicitudMaterial por Bodeguero (PorFicha + InsumosLibres)
[Authorize(Roles = UserRoles.Bodeguero)]
public class BodegaSolicitudesController : Controller
{
    internal const string BodegaNoAsignadaMessage =
        "Su usuario de bodega no tiene ninguna bodega asignada. Pida al administrador que le asigne al menos una para ver y resolver solicitudes.";

    private readonly ISolicitudMaterialService _solicitudService;
    private readonly ISolicitudMaterialApprovalService _approvalService;
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentBodegaAccessor _bodegaAccessor;

    public BodegaSolicitudesController(
        ISolicitudMaterialService solicitudService,
        ISolicitudMaterialApprovalService approvalService,
        IInventoryService inventoryService,
        ICurrentBodegaAccessor bodegaAccessor)
    {
        _solicitudService = solicitudService;
        _approvalService = approvalService;
        _inventoryService = inventoryService;
        _bodegaAccessor = bodegaAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? estado, CancellationToken cancellationToken)
    {
        var soloPendientes = !string.Equals(estado, "todas", StringComparison.OrdinalIgnoreCase);
        var viewerBodegaIds = GetViewerBodegaIds();
        if (viewerBodegaIds is null)
        {
            return View(new BodegaSolicitudesIndexViewModel
            {
                Solicitudes = [],
                SoloPendientes = soloPendientes,
                Message = BodegaNoAsignadaMessage,
                IsSuccess = false
            });
        }

        var list = await _solicitudService.GetListForBodegaAsync(viewerBodegaIds, soloPendientes, cancellationToken);

        return View(new BodegaSolicitudesIndexViewModel
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
        var viewerBodegaIds = GetViewerBodegaIds();
        if (viewerBodegaIds is null)
        {
            TempData["Message"] = BodegaNoAsignadaMessage;
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var detail = await _solicitudService.GetResolucionDetailAsync(id, viewerBodegaIds, cancellationToken);
        if (detail is null)
            return NotFound();

        return View(new BodegaSolicitudDetailViewModel
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
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var bodegueroId))
        {
            TempData["Message"] = "Debe iniciar sesión como bodeguero.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var viewerBodegaIds = GetViewerBodegaIds();
        if (viewerBodegaIds is null)
        {
            TempData["Message"] = BodegaNoAsignadaMessage;
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var scoped = await _solicitudService.GetResolucionDetailAsync(
            form.SolicitudId, viewerBodegaIds, cancellationToken);
        if (scoped is null)
        {
            TempData["Message"] = "La solicitud no pertenece a su bodega.";
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
            bodegueroId,
            form.Observaciones,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud resuelta." : "No se pudo resolver.");
        TempData["IsSuccess"] = result.Success;

        if (result.Success)
            return RedirectToAction(nameof(Index));

        return RedirectToAction(nameof(Detail), new { id = form.SolicitudId });
    }

    // Bodeguero sin asignaciones o sesión no restringida: no se listan todas las bodegas.
    private IReadOnlyList<int>? GetViewerBodegaIds()
    {
        var ids = _bodegaAccessor.BodegaIds;
        if (ids is null || ids.Count == 0)
            return null;
        return ids;
    }
}
