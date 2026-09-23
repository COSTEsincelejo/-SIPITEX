using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
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
        var (_, role, name) = CurrentViewer();
        var form = new CreateActaForm
        {
            ProductionOrderId = orderId,
            Origen = ActaOrigen.Manual,
            EntregaNombre = name ?? string.Empty,
            EntregaCargo = role ?? string.Empty
        };
        return View(await BuildCreateViewModel(form, cancellationToken));
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

        if (!ModelState.IsValid)
            return View(await BuildCreateViewModel(form, cancellationToken));

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
            form.ConsumoIds,
            SignatureImage.FromDataUrl(form.EntregaFirmaDataUrl),
            SignatureImage.FromDataUrl(form.RecibeFirmaDataUrl)), cancellationToken);

        if (result is { Success: true, Value: { } acta })
        {
            TempData["Message"] = result.Message;
            TempData["IsSuccess"] = true;
            return RedirectToAction(nameof(Details), new { id = acta.Id });
        }

        AddActaError(result.Message);
        return View(await BuildCreateViewModel(form, cancellationToken));
    }

    private async Task<ActaCreateViewModel> BuildCreateViewModel(CreateActaForm form, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Nueva acta";
        ViewData["Breadcrumb"] = "SIPITEX / Operación / Actas / Nueva";
        var (userId, role, name) = CurrentViewer();
        var orders = await _orders.GetOrdersAsync(userId, role, name, cancellationToken);
        var selected = form.ProductionOrderId is int id && orders.Any(o => o.Id == id)
            ? id
            : orders.FirstOrDefault()?.Id;
        form.ProductionOrderId = selected;
        return new ActaCreateViewModel
        {
            Orders = orders,
            Movimientos = await _stock.GetHistoryAsync(null, null, null, cancellationToken),
            Consumos = selected is int oid
                ? await _consumos.GetByOrderAsync(oid, cancellationToken)
                : [],
            Form = form
        };
    }

    private void AddActaError(string? message)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "No se pudo registrar el acta." : message;
        if (text.Contains("quien entrega", StringComparison.OrdinalIgnoreCase)
            && text.Contains("quien recibe", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Form.EntregaNombre", "El nombre de quien entrega es obligatorio.");
            ModelState.AddModelError("Form.RecibeNombre", "El nombre de quien recibe es obligatorio.");
            return;
        }

        if (text.Contains("quien entrega", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Form.EntregaFirmaDataUrl", text);
            return;
        }

        if (text.Contains("quien recibe", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Form.RecibeFirmaDataUrl", text);
            return;
        }

        ModelState.AddModelError(string.Empty, text);
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
