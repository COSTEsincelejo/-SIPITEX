using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Cola de SolicitudMaterial: Encargado (su planta), Admin (todas), Instructor (sus grupos, solo consulta).
[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor},{UserRoles.EncargadoDeBodega}")]
public class PlantasInventarioSolicitudesController : Controller
{
    internal const string PlantaInventarioNoAsignadaMessage =
        "Su usuario de planta de inventario no tiene ninguna plantaInventario asignada. Pida al administrador que le asigne al menos una para ver y resolver solicitudes.";

    private readonly ISolicitudMaterialService _solicitudService;
    private readonly ISolicitudMaterialApprovalService _approvalService;
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentPlantaInventarioAccessor _plantaAccessor;
    private readonly IPlantaInventarioService _plantas;

    public PlantasInventarioSolicitudesController(
        ISolicitudMaterialService solicitudService,
        ISolicitudMaterialApprovalService approvalService,
        IInventoryService inventoryService,
        ICurrentPlantaInventarioAccessor plantaAccessor,
        IPlantaInventarioService plantas)
    {
        _solicitudService = solicitudService;
        _approvalService = approvalService;
        _inventoryService = inventoryService;
        _plantaAccessor = plantaAccessor;
        _plantas = plantas;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? estado, CancellationToken cancellationToken)
    {
        var soloPendientes = !string.Equals(estado, "todas", StringComparison.OrdinalIgnoreCase);
        var canResolver = CanResolver();

        if (IsInstructorOnly())
        {
            if (!TryGetActorUserId(out var instructorId))
                return Challenge();

            var list = await _solicitudService.GetListForInstructorGruposAsync(
                instructorId,
                User.Identity?.Name,
                soloPendientes,
                cancellationToken);

            return View(new PlantasInventarioSolicitudesIndexViewModel
            {
                Solicitudes = list,
                SoloPendientes = soloPendientes,
                CanResolver = canResolver,
                Message = TempData["Message"] as string,
                IsSuccess = TempData["IsSuccess"] as bool? ?? false
            });
        }

        var viewerPlantaInventarioIds = await GetViewerPlantaInventarioIdsAsync(cancellationToken);
        if (viewerPlantaInventarioIds is null)
        {
            return View(new PlantasInventarioSolicitudesIndexViewModel
            {
                Solicitudes = [],
                SoloPendientes = soloPendientes,
                CanResolver = canResolver,
                Message = PlantaInventarioNoAsignadaMessage,
                IsSuccess = false
            });
        }

        var scoped = await _solicitudService.GetListForPlantaInventarioAsync(
            viewerPlantaInventarioIds, soloPendientes, cancellationToken);

        return View(new PlantasInventarioSolicitudesIndexViewModel
        {
            Solicitudes = scoped,
            SoloPendientes = soloPendientes,
            CanResolver = canResolver,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var canResolver = CanResolver();
        SolicitudMaterialResolucionDto? detail;

        if (IsInstructorOnly())
        {
            if (!TryGetActorUserId(out var instructorId))
                return Challenge();

            var allowed = await _solicitudService.InstructorCanViewSolicitudAsync(
                id, instructorId, User.Identity?.Name, cancellationToken);
            if (!allowed)
                return NotFound();

            detail = await _solicitudService.GetResolucionDetailAsync(
                id, viewerPlantaInventarioIds: null, unrestricted: true, cancellationToken);
        }
        else
        {
            var viewerPlantaInventarioIds = await GetViewerPlantaInventarioIdsAsync(cancellationToken);
            if (viewerPlantaInventarioIds is null)
            {
                TempData["Message"] = PlantaInventarioNoAsignadaMessage;
                TempData["IsSuccess"] = false;
                return RedirectToAction(nameof(Index));
            }

            detail = await _solicitudService.GetResolucionDetailAsync(
                id, viewerPlantaInventarioIds, unrestricted: false, cancellationToken);
        }

        if (detail is null)
            return NotFound();

        return View(new PlantaInventarioSolicitudDetailViewModel
        {
            Solicitud = detail,
            Materials = canResolver
                ? await _inventoryService.GetMaterialsAsync(cancellationToken)
                : [],
            CanResolver = canResolver,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    public async Task<IActionResult> Resolve(
        [Bind(Prefix = "Resolve")] ResolveSolicitudForm form,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var encargadoDeBodegaId))
        {
            TempData["Message"] = "Debe iniciar sesión como encargado de bodega o administrador.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var viewerPlantaInventarioIds = await GetViewerPlantaInventarioIdsAsync(cancellationToken);
        if (viewerPlantaInventarioIds is null)
        {
            TempData["Message"] = PlantaInventarioNoAsignadaMessage;
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var scoped = await _solicitudService.GetResolucionDetailAsync(
            form.SolicitudId, viewerPlantaInventarioIds, unrestricted: false, cancellationToken);
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

    private bool CanResolver() =>
        User.IsInRole(UserRoles.Administrador) || User.IsInRole(UserRoles.EncargadoDeBodega);

    private bool IsInstructorOnly() =>
        User.IsInRole(UserRoles.Instructor)
        && !User.IsInRole(UserRoles.Administrador)
        && !User.IsInRole(UserRoles.EncargadoDeBodega);

    private async Task<IReadOnlyList<int>?> GetViewerPlantaInventarioIdsAsync(CancellationToken cancellationToken)
    {
        if (User.IsInRole(UserRoles.Administrador))
        {
            var all = await _plantas.GetAllAsync(cancellationToken);
            var ids = all.Select(p => p.Id).Where(id => id > 0).ToList();
            return ids.Count == 0 ? [] : ids;
        }

        var idsEncargado = _plantaAccessor.PlantaInventarioIds;
        if (idsEncargado is null || idsEncargado.Count == 0)
            return null;
        return idsEncargado;
    }

    private bool TryGetActorUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
}
