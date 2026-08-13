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
    private readonly ISolicitudMaterialService _solicitudService;
    private readonly ISolicitudMaterialApprovalService _approvalService;
    private readonly IInventoryService _inventoryService;

    public BodegaSolicitudesController(
        ISolicitudMaterialService solicitudService,
        ISolicitudMaterialApprovalService approvalService,
        IInventoryService inventoryService)
    {
        _solicitudService = solicitudService;
        _approvalService = approvalService;
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? estado, CancellationToken cancellationToken)
    {
        var soloPendientes = !string.Equals(estado, "todas", StringComparison.OrdinalIgnoreCase);
        var list = await _solicitudService.GetListForBodegaAsync(soloPendientes, cancellationToken);

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
        var detail = await _solicitudService.GetResolucionDetailAsync(id, cancellationToken);
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
}
