using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor},{UserRoles.EncargadoDeBodega}")]
public class TrazabilidadController : Controller
{
    private readonly ITrazabilidadService _trazabilidad;
    private readonly IProductionOrderService _orders;

    public TrazabilidadController(ITrazabilidadService trazabilidad, IProductionOrderService orders)
    {
        _trazabilidad = trazabilidad;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int? orderId, int? page, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Trazabilidad de prendas";
        ViewData["Breadcrumb"] = "SIPITEX / Operación / Trazabilidad";
        var (userId, role, name) = CurrentViewer();
        var orders = await _orders.GetOrdersAsync(userId, role, name, cancellationToken);
        var filter = BuildFilter(userId, role, orders.Select(o => o.Id).ToHashSet());
        var selected = orderId is int oid && orders.Any(o => o.Id == oid) ? oid : (int?)null;
        var prendas = await _trazabilidad.SearchAsync(filter, q, selected, page, cancellationToken: cancellationToken);
        return View(new TrazabilidadIndexViewModel
        {
            Query = q,
            OrderId = selected,
            Orders = orders,
            Prendas = prendas.Items,
            Page = prendas.Page,
            PageSize = prendas.PageSize,
            TotalCount = prendas.TotalCount,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar(int orderId, int cantidad, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var userId))
        {
            TempData["Message"] = "No se pudo identificar al usuario.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index), new { orderId });
        }

        var (uid, role, name) = CurrentViewer();
        var orders = await _orders.GetOrdersAsync(uid, role, name, cancellationToken);
        var filter = BuildFilter(uid, role, orders.Select(o => o.Id).ToHashSet());
        var result = await _trazabilidad.GenerarAsync(filter, orderId, cantidad, userId, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index), new { orderId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id, string? codigo, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var orders = await _orders.GetOrdersAsync(userId, role, name, cancellationToken);
        var filter = BuildFilter(userId, role, orders.Select(o => o.Id).ToHashSet());
        var detail = id is int pid
            ? await _trazabilidad.GetByIdAsync(filter, pid, cancellationToken)
            : await _trazabilidad.GetByCodigoAsync(filter, codigo ?? string.Empty, cancellationToken);
        if (detail is null)
            return NotFound();

        ViewData["Title"] = detail.Codigo;
        ViewData["Breadcrumb"] = $"SIPITEX / Operación / Trazabilidad / {detail.Codigo}";
        return View(detail);
    }

    private static TrazabilidadViewerFilter BuildFilter(int? userId, string? role, IReadOnlyCollection<int> orderIds) =>
        new(role ?? string.Empty, userId ?? 0, orderIds);

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
