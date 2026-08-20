using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Solicitudes multi-ítem: PorFicha (desde Fichas) e InsumosLibres (pantalla dedicada)
[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
public class SolicitudesMaterialController : Controller
{
    private readonly ISolicitudMaterialService _solicitudService;
    private readonly IFichaService _fichaService;
    private readonly IProductionOrderService _orderService;

    public SolicitudesMaterialController(
        ISolicitudMaterialService solicitudService,
        IFichaService fichaService,
        IProductionOrderService orderService)
    {
        _solicitudService = solicitudService;
        _fichaService = fichaService;
        _orderService = orderService;
    }

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

    // Pantalla nueva: solicitar insumos por descripción libre
    [HttpGet]
    public async Task<IActionResult> SolicitarInsumos(CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        return View(await BuildSolicitarInsumosVm(userId, role, name, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarInsumos(
        [Bind(Prefix = "Form")] CreateInsumosLibresForm form,
        CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        if (userId is not int solicitanteId)
        {
            TempData["Message"] = "Debe iniciar sesión para solicitar insumos.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(SolicitarInsumos));
        }

        var detalles = (form.Detalles ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d.DescripcionItem) && d.CantidadSolicitada > 0)
            .Select(d => new CreateDetalleSolicitudDto(null, d.CantidadSolicitada, d.DescripcionItem.Trim()))
            .ToList();

        var result = await _solicitudService.CreateAsync(
            new CreateSolicitudMaterialDto(
                SolicitudMaterialTipo.InsumosLibres,
                form.FichaId is > 0 ? form.FichaId : null,
                form.ProductionOrderId is > 0 ? form.ProductionOrderId : null,
                form.DescripcionLibre,
                detalles,
                form.Observaciones,
                form.BodegaId),
            solicitanteId,
            role,
            name,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud creada." : "No se pudo crear la solicitud.");
        TempData["IsSuccess"] = result.Success;

        if (result.Success)
            return RedirectToAction(nameof(Index));

        var vm = await BuildSolicitarInsumosVm(userId, role, name, cancellationToken, form);
        vm.Message = result.Message;
        vm.IsSuccess = false;
        return View(vm);
    }

    // Create PorFicha desde Fichas (comportamiento histórico)
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
            new CreateSolicitudMaterialDto(
                SolicitudMaterialTipo.PorFicha,
                form.FichaId,
                ProductionOrderId: null,
                DescripcionLibre: null,
                detalles,
                form.Observaciones),
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

    private async Task<SolicitarInsumosViewModel> BuildSolicitarInsumosVm(
        int? userId,
        string? role,
        string? name,
        CancellationToken cancellationToken,
        CreateInsumosLibresForm? form = null)
    {
        var fichas = await _fichaService.GetFichasAsync(userId, role, name, cancellationToken);
        var orders = await _orderService.GetOrdersAsync(userId, role, name, cancellationToken);
        return new SolicitarInsumosViewModel
        {
            Form = form ?? new CreateInsumosLibresForm(),
            Fichas = fichas.Select(f => (f.Id, f.FichaCode)).ToList(),
            Ordenes = orders.Select(o => (o.Id, $"{o.OrderNumber} · {o.ProductName}")).ToList(),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
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
