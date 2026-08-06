using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Órdenes de producción: listar, crear, materiales opcionales y registrar avance
[Authorize]
public class OrdenesController : Controller
{
    private readonly IProductionOrderService _orderService;
    private readonly IBomCatalogService _bomCatalog;
    private readonly IOrderMaterialService _orderMaterialService;
    private readonly IInventoryService _inventoryService;

    public OrdenesController(
        IProductionOrderService orderService,
        IBomCatalogService bomCatalog,
        IOrderMaterialService orderMaterialService,
        IInventoryService inventoryService)
    {
        _orderService = orderService;
        _bomCatalog = bomCatalog;
        _orderMaterialService = orderMaterialService;
        _inventoryService = inventoryService;
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

    // Detalle de materiales asociados (extensión; no altera Create/AddProduction)
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var detail = await _orderMaterialService.GetDetailAsync(id, cancellationToken);
        if (detail is null) return NotFound();

        return View(new OrdenMaterialDetailViewModel
        {
            Detail = detail,
            Materials = await _inventoryService.GetMaterialsAsync(cancellationToken),
            AddMaterial = new AddOrderMaterialForm { OrderId = id, QuantityRequired = 1 },
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
    public async Task<IActionResult> AddProduction(int id, int units, CancellationToken cancellationToken)
    {
        var result = await _orderService.RegisterProductionAsync(id, units, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Producción registrada." : "Error en producción.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaterial([Bind(Prefix = "AddMaterial")] AddOrderMaterialForm form, CancellationToken cancellationToken)
    {
        var result = await _orderMaterialService.AddMaterialAsync(
            new AddOrderMaterialDto(form.OrderId, form.MaterialId, form.QuantityRequired, form.Observations),
            cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id = form.OrderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMaterial(int lineId, int orderId, CancellationToken cancellationToken)
    {
        var result = await _orderMaterialService.RemoveMaterialAsync(lineId, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportBomMaterials(int id, CancellationToken cancellationToken)
    {
        var result = await _orderMaterialService.ImportFromBomAsync(id, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id });
    }
}
