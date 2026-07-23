using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class OrdenesController : Controller
{
    private readonly IProductionOrderService _orderService;

    public OrdenesController(IProductionOrderService orderService) => _orderService = orderService;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await BuildViewModel(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderForm form, CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateOrderAsync(
            new CreateProductionOrderDto(form.ProductName, form.TotalQuantity, form.Deadline), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Orden creada." : "Error al crear orden.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduction(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.RegisterProductionAsync(id, 10, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Producción registrada." : "Error en producción.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    private async Task<OrdenesIndexViewModel> BuildViewModel(CancellationToken cancellationToken) =>
        new()
        {
            Orders = await _orderService.GetOrdersAsync(cancellationToken),
            KnownProducts = await _orderService.GetKnownProductNamesAsync(cancellationToken),
            CreateOrder = new CreateOrderForm(),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
}
