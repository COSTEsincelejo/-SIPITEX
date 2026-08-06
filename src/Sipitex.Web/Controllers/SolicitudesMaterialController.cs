using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Solicitudes multi-ítem ligadas a Ficha (flujo paralelo a Inventario/MaterialRequest)
[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
public class SolicitudesMaterialController : Controller
{
    private readonly ISolicitudMaterialService _solicitudService;

    public SolicitudesMaterialController(ISolicitudMaterialService solicitudService) =>
        _solicitudService = solicitudService;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var (userId, role, _) = CurrentViewer();
        var list = await _solicitudService.GetListAsync(userId, role, cancellationToken);
        return View(new SolicitudesMaterialIndexViewModel
        {
            Solicitudes = list,
            IsAdministrator = User.IsInRole(UserRoles.Administrador),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var (userId, role, _) = CurrentViewer();
        var detail = await _solicitudService.GetDetailAsync(id, userId, role, cancellationToken);
        if (detail is null)
            return NotFound();

        return View(new SolicitudMaterialDetailViewModel
        {
            Solicitud = detail,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "CreateSolicitud")] CreateSolicitudMaterialForm form,
        CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        if (userId is not int solicitanteId)
        {
            TempData["Message"] = "Debe iniciar sesión para solicitar materiales.";
            TempData["IsSuccess"] = false;
            return RedirectToAction("Index", "Fichas");
        }

        var detalles = (form.Detalles ?? [])
            .Where(d => d.MaterialId > 0 && d.CantidadSolicitada > 0)
            .Select(d => new CreateDetalleSolicitudDto(d.MaterialId, d.CantidadSolicitada))
            .ToList();

        var result = await _solicitudService.CreateAsync(
            new CreateSolicitudMaterialDto(form.FichaId, detalles, form.Observaciones),
            solicitanteId,
            role,
            name,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud creada." : "No se pudo crear la solicitud.");
        TempData["IsSuccess"] = result.Success;

        if (result.Success)
            return RedirectToAction(nameof(Index));

        return RedirectToAction("Index", "Fichas");
    }

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            userId = id;

        var role = User.FindFirstValue(ClaimTypes.Role);
        var name = User.FindFirstValue(ClaimTypes.Name);
        return (userId, role, name);
    }
}
