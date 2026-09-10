using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor},{UserRoles.EncargadoDeBodega}")]
public class ActasController : Controller
{
    private readonly IActaMovimientoService _actas;
    private readonly IProductionOrderService _orders;
    private readonly IStockMovementService _stock;
    private readonly IMaterialConsumptionService _consumos;
    private readonly ICurrentPlantaInventarioAccessor _plantaAccessor;

    public ActasController(
        IActaMovimientoService actas,
        IProductionOrderService orders,
        IStockMovementService stock,
        IMaterialConsumptionService consumos,
        ICurrentPlantaInventarioAccessor plantaAccessor)
    {
        _actas = actas;
        _orders = orders;
        _stock = stock;
        _consumos = consumos;
        _plantaAccessor = plantaAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Actas de ingreso y egreso";
        ViewData["Breadcrumb"] = "SIPITEX / Operación / Actas";
        var filter = await BuildFilterAsync(cancellationToken);
        return View(new ActasIndexViewModel
        {
            Actas = await _actas.GetAllAsync(filter, cancellationToken),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? orderId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Nueva acta";
        ViewData["Breadcrumb"] = "SIPITEX / Operación / Actas / Nueva";
        var (userId, role, name) = CurrentViewer();
        var orders = await _orders.GetOrdersAsync(userId, role, name, cancellationToken);
        var selected = orderId is int id && orders.Any(o => o.Id == id) ? id : orders.FirstOrDefault()?.Id;
        var vm = new ActaCreateViewModel
        {
            Orders = orders,
            Movimientos = await _stock.GetHistoryAsync(null, null, null, cancellationToken),
            Consumos = selected is int oid
                ? await _consumos.GetByOrderAsync(oid, cancellationToken)
                : [],
            Form = new CreateActaForm
            {
                ProductionOrderId = selected,
                Origen = ActaOrigen.Manual,
                EntregaNombre = name ?? string.Empty,
                EntregaCargo = role ?? string.Empty
            }
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] CreateActaForm form, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var userId))
        {
            TempData["Message"] = "No se pudo identificar al usuario.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var result = await _actas.CreateAsync(new CreateActaDto(
            form.Tipo,
            form.Origen,
            form.EntregaNombre,
            form.EntregaCargo,
            form.EntregaConforme,
            form.RecibeNombre,
            form.RecibeCargo,
            form.RecibeConforme,
            userId,
            form.Observaciones,
            form.ProductionOrderId,
            form.EstadoOrigen,
            form.EstadoDestino,
            form.StockMovementIds,
            form.ConsumoIds), cancellationToken);

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        if (result is { Success: true, Value: { } acta })
            return RedirectToAction(nameof(Details), new { id = acta.Id });
        return RedirectToAction(nameof(Create), new { orderId = form.ProductionOrderId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var filter = await BuildFilterAsync(cancellationToken);
        var acta = await _actas.GetByIdAsync(id, filter, cancellationToken);
        if (acta is null)
            return NotFound();
        ViewData["Title"] = acta.Numero;
        ViewData["Breadcrumb"] = $"SIPITEX / Operación / Actas / {acta.Numero}";
        return View(acta);
    }

    [HttpGet]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken)
    {
        var filter = await BuildFilterAsync(cancellationToken);
        var visible = await _actas.GetByIdAsync(id, filter, cancellationToken);
        if (visible is null)
            return NotFound();
        var result = await _actas.ExportPdfAsync(id, cancellationToken);
        if (!result.Success || result.Value is null)
            return NotFound();
        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    private async Task<ActaViewerFilter?> BuildFilterAsync(CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        if (userId is not int uid || string.IsNullOrWhiteSpace(role))
            return null;
        if (string.Equals(role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return new ActaViewerFilter(role, uid, [], []);

        IReadOnlyList<int> plantas = _plantaAccessor.PlantaInventarioIds ?? [];
        IReadOnlyCollection<int> orders = [];
        if (string.Equals(role, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
        {
            var list = await _orders.GetOrdersAsync(uid, role, name, cancellationToken);
            orders = list.Select(o => o.Id).ToHashSet();
        }

        return new ActaViewerFilter(role, uid, plantas, orders);
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
