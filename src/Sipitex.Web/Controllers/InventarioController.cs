using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Materiales, stock, solicitudes y aprobaciones del inventario
[Authorize]
public class InventarioController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IProductionOrderService _orderService;

    public InventarioController(IInventoryService inventoryService, IProductionOrderService orderService)
    {
        _inventoryService = inventoryService;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await BuildViewModel(cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeRegistrarMateriales)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaterial([Bind(Prefix = "CreateMaterial")] CreateMaterialForm form, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AddMaterialAsync(
            new CreateMaterialDto(form.Name, form.Stock, form.Unit), cancellationToken);

        // Devuelvo la misma vista con mensaje en vez de redirect (el form queda en la página)
        var vm = await BuildViewModel(cancellationToken);
        vm.Message = result.Message ?? (result.Success ? "Material agregado." : "Error al agregar material.");
        vm.IsSuccess = result.Success;
        return View("Index", vm);
    }

    // Ajuste de stock (bodega/admin). Uso TempData porque hago redirect.
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(AdjustStockForm form, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AdjustStockAsync(
            new AdjustStockDto(form.MaterialId, form.NewStock), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Stock actualizado." : "Error al ajustar stock.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int MaterialId, MaterialStatus Status, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.UpdateStatusAsync(
            new UpdateMaterialStatusDto(MaterialId, Status), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Estado actualizado." : "Error al actualizar estado.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Los instructores piden material para una orden; bodega/admin aprueba después
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest([Bind(Prefix = "CreateRequest")] CreateRequestForm form, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.CreateRequestAsync(
            new CreateMaterialRequestDto(form.ProductionOrderId, form.MaterialId, form.Quantity), cancellationToken);

        var vm = await BuildViewModel(cancellationToken);
        vm.Message = result.Message ?? (result.Success ? "Solicitud creada." : "Error al crear solicitud.");
        vm.IsSuccess = result.Success;
        return View("Index", vm);
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeAprobarSolicitudes)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.ApproveRequestAsync(id, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud aprobada." : "No se pudo aprobar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeAprobarSolicitudes)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.RejectRequestAsync(id, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud rechazada." : "No se pudo rechazar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Arma el ViewModel completo de la pantalla (materiales + solicitudes + combos)
    private async Task<InventarioIndexViewModel> BuildViewModel(CancellationToken cancellationToken)
    {
        var materials = await _inventoryService.GetMaterialsAsync(cancellationToken);
        var orders = await _orderService.GetOrdersAsync(cancellationToken);

        return new InventarioIndexViewModel
        {
            Materials = materials,
            Requests = await _inventoryService.GetRequestsAsync(cancellationToken),
            Orders = orders,
            CreateMaterial = new CreateMaterialForm(),
            // Prefiero dejar seleccionado el primer ítem para que el form no quede vacío
            CreateRequest = new CreateRequestForm
            {
                ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0,
                MaterialId = materials.FirstOrDefault()?.Id ?? 0
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }
}
