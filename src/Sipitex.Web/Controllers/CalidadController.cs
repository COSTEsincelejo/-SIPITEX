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
        // Necesito las órdenes para el dropdown del form (Instructor: solo asignadas)
        var orders = await _orderService.GetOrdersAsync(userId, role, name, cancellationToken);
        // Armo el ViewModel de la pantalla completa
        return View(new CalidadIndexViewModel
        {
            // Historial de inspecciones
            Records = await _qualityService.GetRecordsAsync(cancellationToken),
            // Combo de órdenes de producción
            Orders = orders,
            // Form con la primera orden ya elegida
            // Preselecciono la primera orden para que el combo no quede en cero
            Create = new CreateQualityForm { ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0 },
            // Mensaje del último POST
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

        // Mando el DTO al servicio para guardar la inspección
        var result = await _qualityService.AddRecordAsync(
            new CreateQualityRecordDto(
                form.ProductionOrderId,
                form.Units,
                form.Result,
                form.MotivoReproceso,
                form.Responsable),
            cancellationToken);

        // Mensaje flash según resultado del servicio
        TempData["Message"] = result.Message ?? (result.Success ? "Inspección registrada." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        // Vuelvo al listado para ver la nueva fila
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
