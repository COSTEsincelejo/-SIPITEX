using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Materiales de órdenes: Encargado/Admin operan; Instructor consulta las suyas.
[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor},{UserRoles.EncargadoDeBodega}")]
public class PlantasInventarioOrdenesController : Controller
{
    private readonly IOrderMaterialService _orderMaterialService;
    private readonly IProductionOrderService _orderService;
    private readonly IProductionFlowService _flowService;
    private readonly IInventoryService _inventoryService;

    public PlantasInventarioOrdenesController(
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
        var orders = await _orderMaterialService.GetOrdersForPlantaInventarioAsync(cancellationToken);
        if (IsInstructorOnly())
        {
            var (userId, role, name) = CurrentViewer();
            var allowed = await _orderService.GetOrdersAsync(userId, role, name, cancellationToken);
            var allowedIds = allowed.Select(o => o.Id).ToHashSet();
            orders = orders.Where(o => allowedIds.Contains(o.Id)).ToList();
        }

        return View(new PlantasInventarioOrdenesIndexViewModel
        {
            Orders = orders,
            CanEntregar = CanOperarPlanta(),
            CanReingresar = CanOperarPlanta(),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        if (!await CanViewOrderAsync(id, cancellationToken))
            return NotFound();

        var detail = await _orderMaterialService.GetDetailAsync(id, cancellationToken);
        if (detail is null) return NotFound();
        if (detail.MaterialsStatus == Domain.Enums.OrderMaterialsStatus.NoAplica)
            return RedirectToAction(nameof(Index));

        return View(new PlantaInventarioOrdenDetailViewModel
        {
            Detail = detail,
            CanEntregar = CanOperarPlanta(),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    public async Task<IActionResult> Reingreso(int? orderId, CancellationToken cancellationToken)
    {
        return View(await BuildReingresoViewModel(orderId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    public async Task<IActionResult> Reingreso(
        [Bind(Prefix = "Form")] PlantaInventarioReingresoForm form,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorId, out var nombre))
        {
            TempData["Message"] = "Sesión no válida.";
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
            actorId,
            nombre,
            User.FindFirstValue(ClaimTypes.Role) ?? UserRoles.EncargadoDeBodega,
            cancellationToken);

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Reingreso), new { orderId = form.OrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    public async Task<IActionResult> ValidateStock(int id, CancellationToken cancellationToken)
    {
        var result = await _orderMaterialService.ValidateStockAsync(id, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    public async Task<IActionResult> Deliver([Bind(Prefix = "Deliver")] DeliverOrderMaterialsForm form, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var encargadoDeBodegaId))
        {
            TempData["Message"] = "Sesión no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Index));
        }

        var items = (form.Items ?? [])
            .Select(i => new DeliverOrderMaterialItemDto(i.LineId, i.QuantityToDeliver))
            .ToList();

        var result = await _orderMaterialService.DeliverAsync(
            new DeliverOrderMaterialsDto(form.OrderId, items, form.Observations),
            encargadoDeBodegaId,
            cancellationToken);

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id = form.OrderId });
    }

    private async Task<PlantaInventarioReingresoViewModel> BuildReingresoViewModel(
        int? orderId,
        CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var orders = await _orderService.GetOrdersAsync(userId, role, name, cancellationToken);
        var materials = await _inventoryService.GetMaterialsAsync(cancellationToken);
        IReadOnlyList<OrderStageDto> stages = [];

        if (orderId is int oid and > 0)
        {
            var mes = await _flowService.GetMesDetailAsync(
                oid, userId, role ?? UserRoles.EncargadoDeBodega, cancellationToken);
            if (mes is not null)
            {
                stages = mes.Stages
                    .Where(s => ProductionFlowService.DefaultStageNames.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        return new PlantaInventarioReingresoViewModel
        {
            Orders = orders,
            Materials = materials,
            Stages = stages,
            StageNames = ProductionFlowService.DefaultStageNames,
            Form = new PlantaInventarioReingresoForm
            {
                OrderId = orderId ?? 0,
                Quantity = 1
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }

    private async Task<bool> CanViewOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        if (CanOperarPlanta())
            return true;

        var (userId, role, name) = CurrentViewer();
        return await _orderService.CanAccessOrderAsync(orderId, userId, role, name, cancellationToken);
    }

    private bool CanOperarPlanta() =>
        User.IsInRole(UserRoles.Administrador) || User.IsInRole(UserRoles.EncargadoDeBodega);

    private bool IsInstructorOnly() =>
        User.IsInRole(UserRoles.Instructor)
        && !User.IsInRole(UserRoles.Administrador)
        && !User.IsInRole(UserRoles.EncargadoDeBodega);

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            userId = id;
        return (userId, User.FindFirstValue(ClaimTypes.Role), User.FindFirstValue(ClaimTypes.Name));
    }

    private bool TryGetActor(out int userId, out string nombre)
    {
        userId = 0;
        nombre = User.Identity?.Name ?? "Encargado de bodega";
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
    }
}
