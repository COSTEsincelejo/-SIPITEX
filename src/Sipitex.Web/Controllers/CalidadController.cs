using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class CalidadController : Controller
{
    // Este controlador permite ver y registrar la calidad de la producción.
    private readonly IQualityService _qualityService;
    private readonly IProductionOrderService _orderService;

    public CalidadController(IQualityService qualityService, IProductionOrderService orderService)
    {
        _qualityService = qualityService;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersAsync(cancellationToken);
        return View(new CalidadIndexViewModel
        {
            Records = await _qualityService.GetRecordsAsync(cancellationToken),
            Orders = orders,
            Create = new CreateQualityForm { ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0 },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateQualityForm form, CancellationToken cancellationToken)
    {
        var result = await _qualityService.AddRecordAsync(
            new CreateQualityRecordDto(
                form.ProductionOrderId,
                form.Units,
                form.Result,
                form.MotivoReproceso,
                form.Responsable),
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Inspección registrada." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }
}
