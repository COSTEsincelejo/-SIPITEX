using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Cola de bodega para materiales asociados a órdenes de producción (extensión)
[Authorize(Roles = UserRoles.Bodeguero)]
public class BodegaOrdenesController : Controller
{
    private readonly IOrderMaterialService _orderMaterialService;
    private readonly IProductionOrderService _orderService;
    private readonly IProductionFlowService _flowService;
    private readonly IInventoryService _inventoryService;

    public BodegaOrdenesController(
        IOrderMaterialService orderMaterialService,
        IProductionOrderService orderService,
        IProductionFlowService flowService,
        IInventoryService inventoryService)
    {
        _orderMaterialService = orderMaterialService;
        _orderService = orderService;
        _flowService = flowService;
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new BodegaOrdenesIndexViewModel
        {
            Orders = await _orderMaterialService.GetOrdersForBodegaAsync(cancellationToken),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var detail = await _orderMaterialService.GetDetailAsync(id, cancellationToken);
        if (detail is null) return NotFound();
        if (detail.MaterialsStatus == Domain.Enums.OrderMaterialsStatus.NoAplica)
            return RedirectToAction(nameof(Index));

        return View(new BodegaOrdenDetailViewModel
        {
            Detail = detail,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    // Gap #14: reingreso desde etapas MES hacia bodega / inventario terminado
    [HttpGet]
    public async Task<IActionResult> Reingreso(int? orderId, CancellationToken cancellationToken)
    {
        return View(await BuildReingresoViewModel(orderId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reingreso(
        [Bind(Prefix = "Form")] BodegaReingresoForm form,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var bodegueroId, out var nombre))
        {
            TempData["Message"] = "Sesión de bodeguero no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Reingreso), new { orderId = form.OrderId });
        }

        int? materialId = form.EsProductoTerminado ? null : form.MaterialId;
        if (!form.EsProductoTerminado && form.MaterialId <= 0)
        {
            TempData["Message"] = "Seleccione el material que reingresa.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Reingreso), new { orderId = form.OrderId });
        }

        var result = await _flowService.RegisterStageReentryAsync(
            new StageReentryDto(form.OrderId, form.StageId, form.Quantity, materialId, form.Observations),
            bodegueroId,
            nombre,
            UserRoles.Bodeguero,
            cancellationToken);

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Reingreso), new { orderId = form.OrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateStock(int id, CancellationToken cancellationToken)
    {
        var result = await _orderMaterialService.ValidateStockAsync(id, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver([Bind(Prefix = "Deliver")] DeliverOrderMaterialsForm form, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var bodegueroId))
        {
            TempData["Message"] = "Sesión de bodeguero no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var items = (form.Items ?? [])
            .Select(i => new DeliverOrderMaterialItemDto(i.LineId, i.QuantityToDeliver))
            .ToList();

        var result = await _orderMaterialService.DeliverAsync(
            new DeliverOrderMaterialsDto(form.OrderId, items, form.Observations),
            bodegueroId,
            cancellationToken);

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id = form.OrderId });
    }

    private async Task<BodegaReingresoViewModel> BuildReingresoViewModel(
        int? orderId,
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersAsync(cancellationToken: cancellationToken);
        var materials = await _inventoryService.GetMaterialsAsync(cancellationToken);
        IReadOnlyList<OrderStageDto> stages = [];

        if (orderId is int oid and > 0)
        {
            var uid = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
            var mes = await _flowService.GetMesDetailAsync(oid, uid, UserRoles.Bodeguero, cancellationToken);
            if (mes is not null)
            {
                stages = mes.Stages
                    .Where(s => ProductionFlowService.DefaultStageNames.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        return new BodegaReingresoViewModel
        {
            Orders = orders,
            Materials = materials,
            Stages = stages,
            StageNames = ProductionFlowService.DefaultStageNames,
            Form = new BodegaReingresoForm
            {
                OrderId = orderId ?? 0,
                Quantity = 1
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }

    private bool TryGetActor(out int userId, out string nombre)
    {
        userId = 0;
        nombre = User.Identity?.Name ?? "Bodeguero";
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
    }
}
