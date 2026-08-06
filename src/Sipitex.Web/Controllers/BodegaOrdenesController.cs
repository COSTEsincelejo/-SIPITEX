using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Cola de bodega para materiales asociados a órdenes de producción (extensión)
[Authorize(Roles = UserRoles.Bodeguero)]
public class BodegaOrdenesController : Controller
{
    private readonly IOrderMaterialService _orderMaterialService;

    public BodegaOrdenesController(IOrderMaterialService orderMaterialService) =>
        _orderMaterialService = orderMaterialService;

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
}
