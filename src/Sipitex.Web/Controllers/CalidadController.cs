using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Registro de inspecciones de calidad ligadas a órdenes de producción
[Authorize]
public class CalidadController : Controller
{
    private readonly IQualityService _qualityService;
    private readonly IProductionOrderService _orderService;

    // Inyecto calidad y órdenes porque el form necesita el combo de órdenes
    public CalidadController(IQualityService qualityService, IProductionOrderService orderService)
    {
        _qualityService = qualityService;
        _orderService = orderService;
    }

    // Lista inspecciones y deja el form para registrar una nueva
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        // Instructor: solo órdenes asignadas (mismo alcance que PR #47)
        var orders = await _orderService.GetOrdersAsync(userId, role, name, cancellationToken);
        var lastOrderId = TempData["LastOrderId"] as int?;
        var selectedOrderId = lastOrderId.HasValue && orders.Any(order => order.Id == lastOrderId.Value)
            ? lastOrderId.Value
            : orders.FirstOrDefault()?.Id ?? 0;
        return View(new CalidadIndexViewModel
        {
            Records = await _qualityService.GetRecordsAsync(userId, role, name, cancellationToken),
            Orders = orders,
            Create = new CreateQualityForm { ProductionOrderId = selectedOrderId, Responsable = name },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    // Registra una inspección (aprobado/rechazado, unidades, responsable)
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Create")] CreateQualityForm form, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        if (!await _orderService.CanAccessOrderAsync(form.ProductionOrderId, userId, role, name, cancellationToken))
            return Forbid();

        var result = await _qualityService.AddRecordAsync(
            new CreateQualityRecordDto(
                form.ProductionOrderId,
                form.Units,
                form.Result,
                form.MotivoReproceso,
                form.Responsable,
                form.Date,
                form.Clasificacion),
            userId,
            role,
            name,
            cancellationToken);

        if (!result.Success)
        {
            var orders = await _orderService.GetOrdersAsync(userId, role, name, cancellationToken);
            return View("Index", new CalidadIndexViewModel
            {
                Records = await _qualityService.GetRecordsAsync(userId, role, name, cancellationToken),
                Orders = orders,
                Create = form,
                Message = result.Message ?? "Error al registrar.",
                IsSuccess = false
            });
        }

        TempData["LastOrderId"] = form.ProductionOrderId;
        TempData["Message"] = result.Message ?? "Inspección registrada.";
        TempData["IsSuccess"] = true;
        return RedirectToAction(nameof(Index));
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
