using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Órdenes de producción: listar, crear y registrar avance
[Authorize]
public class OrdenesController : Controller
{
    private readonly IProductionOrderService _orderService;
    private readonly IBomCatalogService _bomCatalog;

    public OrdenesController(IProductionOrderService orderService, IBomCatalogService bomCatalog)
    {
        _orderService = orderService;
        _bomCatalog = bomCatalog;
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new OrdenesIndexViewModel
        {
            Orders = await _orderService.GetOrdersAsync(cancellationToken),
            ProductNames = await _bomCatalog.GetOrderEligibleProductNamesAsync(cancellationToken),
            CreateOrder = new CreateOrderForm(),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateOrder")] CreateOrderForm form, CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateOrderAsync(
            new CreateProductionOrderDto(form.ProductName, form.TotalQuantity, form.Deadline), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Orden creada." : "Error al crear orden.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduction(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.RegisterProductionAsync(id, 10, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Producción registrada." : "Error en producción.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }
}
