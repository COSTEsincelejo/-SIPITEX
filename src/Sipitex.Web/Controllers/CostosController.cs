using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
public class CostosController : Controller
{
    private readonly IGarmentCostingService _costing;
    private readonly ICostingSettingsService _costingSettings;
    private readonly IProductionOrderService _orders;

    public CostosController(
        IGarmentCostingService costing,
        ICostingSettingsService costingSettings,
        IProductionOrderService orders)
    {
        _costing = costing;
        _costingSettings = costingSettings;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? orderId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Costeo de prendas";
        ViewData["Breadcrumb"] = "SIPITEX / Análisis / Costeo";
        var (userId, role, name) = CurrentViewer();
        var orders = await _orders.GetOrdersAsync(userId, role, name, cancellationToken);
        var selected = orderId is int id && orders.Any(o => o.Id == id)
            ? id
            : orders.FirstOrDefault()?.Id;
        var tarifa = await _costingSettings.GetLaborHourRateAsync(cancellationToken);
        var costo = selected is int oid
            ? await _costing.CalcularAsync(oid, cancellationToken)
            : null;
        return View(new CostosIndexViewModel
        {
            Orders = orders,
            OrderId = selected,
            Costo = costo,
            LaborHourRate = tarifa,
            LaborHourRateUnconfigured = tarifa <= 0,
            CanEditRate = User.IsInRole(UserRoles.Administrador),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Tarifa(decimal laborHourRate, int? orderId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId <= 0)
        {
            TempData["Message"] = "No se pudo identificar al usuario.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index), new { orderId });
        }

        var result = await _costingSettings.UpdateLaborHourRateAsync(laborHourRate, userId, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index), new { orderId });
    }

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            userId = id;
        return (userId, User.FindFirstValue(ClaimTypes.Role), User.FindFirstValue(ClaimTypes.Name));
    }
}
