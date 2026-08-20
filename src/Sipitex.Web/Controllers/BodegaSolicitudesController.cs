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
    private readonly IUserAccountService _userAccountService;

    public BodegaSolicitudesController(
        ISolicitudMaterialService solicitudService,
        ISolicitudMaterialApprovalService approvalService,
        IInventoryService inventoryService,
        IUserAccountService userAccountService)
    {
        _solicitudService = solicitudService;
        _approvalService = approvalService;
        _inventoryService = inventoryService;
        _userAccountService = userAccountService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? estado, CancellationToken cancellationToken)
    {
        var soloPendientes = !string.Equals(estado, "todas", StringComparison.OrdinalIgnoreCase);

        var (bodegaId, errorMessage) = await TryGetAssignedBodegaIdAsync(cancellationToken);
        if (errorMessage is not null)
        {
            return View(new BodegaSolicitudesIndexViewModel
            {
                Solicitudes = [],
                SoloPendientes = soloPendientes,
                Message = errorMessage,
                IsSuccess = false
            });
        }

        var list = await _solicitudService.GetListForBodegaAsync(bodegaId!.Value, soloPendientes, cancellationToken);

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
        var (bodegaId, errorMessage) = await TryGetAssignedBodegaIdAsync(cancellationToken);
        if (errorMessage is not null)
        {
            TempData["Message"] = errorMessage;
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var detail = await _solicitudService.GetResolucionDetailAsync(id, bodegaId!.Value, cancellationToken);
        if (detail is null)
            return NotFound();

        return View(new BodegaSolicitudDetailViewModel
        {
            Solicitud = detail,
            Materials = await _inventoryService.GetMaterialsAsync(bodegaId: bodegaId, cancellationToken: cancellationToken),
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

    private async Task<(int? BodegaId, string? ErrorMessage)> TryGetAssignedBodegaIdAsync(
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var bodegueroId))
            return (null, "Debe iniciar sesión como bodeguero.");

        var user = await _userAccountService.GetUserByIdAsync(bodegueroId, cancellationToken);
        if (user?.BodegaId is not int bodegaId)
            return (null, "Su cuenta no tiene una bodega asignada. Contacte al administrador.");

        return (bodegaId, null);
    }
}
