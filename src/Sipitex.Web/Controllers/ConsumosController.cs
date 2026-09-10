using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
public class ConsumosController : Controller
{
    private readonly IMaterialConsumptionService _consumos;
    private readonly IProductionOrderService _orders;
    private readonly IInventoryService _inventory;
    private readonly IActivityLogService _activityLog;

    public ConsumosController(
        IMaterialConsumptionService consumos,
        IProductionOrderService orders,
        IInventoryService inventory,
        IActivityLogService activityLog)
    {
        _consumos = consumos;
        _orders = orders;
        _inventory = inventory;
        _activityLog = activityLog;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? orderId, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var orders = await _orders.GetOrdersAsync(userId, role, name, cancellationToken);
        var selected = orderId is int id && orders.Any(o => o.Id == id)
            ? id
            : orders.FirstOrDefault()?.Id ?? 0;

        var consumos = selected > 0
            ? await _consumos.GetByOrderAsync(selected, cancellationToken)
            : [];
        var costo = selected > 0
            ? await _consumos.GetCostoPromedioByOrderAsync(selected, cancellationToken)
            : null;

        return View(new ConsumosIndexViewModel
        {
            Orders = orders,
            Materials = await _inventory.GetMaterialsAsync(cancellationToken),
            Consumos = consumos,
            Costo = costo,
            Form = new RegisterConsumoForm { ProductionOrderId = selected },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] RegisterConsumoForm form, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorId))
        {
            TempData["Message"] = "Sesión no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index), new { orderId = form.ProductionOrderId });
        }

        var (userId, role, name) = CurrentViewer();
        if (!await _orders.CanAccessOrderAsync(form.ProductionOrderId, userId, role, name, cancellationToken))
            return Forbid();

        DateTime? fechaUtc = form.FechaLocal is DateTime local
            ? DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime()
            : null;

        var result = await _consumos.RegisterAsync(
            new RegisterConsumoMaterialDto(
                form.ProductionOrderId,
                form.MaterialId,
                form.Cantidad,
                actorId,
                FechaUtc: fechaUtc),
            cancellationToken);

        if (result.Success)
        {
            await _activityLog.LogAsync(
                actorId,
                ActivityLogActions.RegisterConsumoMaterial,
                ActivityLogEntities.ConsumoMaterial,
                entityId: form.ProductionOrderId.ToString(),
                details: result.Message,
                cancellationToken);
        }

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index), new { orderId = form.ProductionOrderId });
    }

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            userId = id;
        return (userId, User.FindFirstValue(ClaimTypes.Role), User.FindFirstValue(ClaimTypes.Name));
    }

    private bool TryGetActorUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
}
