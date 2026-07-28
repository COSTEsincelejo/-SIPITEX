using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Enums;
using Sipitex.Web.Helpers;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

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
    public async Task<IActionResult> Index(string? filtro, CancellationToken cancellationToken)
    {
        return View(await BuildViewModel(filtro, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaterial([Bind(Prefix = "CreateMaterial")] CreateMaterialForm form, CancellationToken cancellationToken)
    {
        if (!PermissionHelper.CanRegisterMaterials(User))
            return Forbid();

        var result = await _inventoryService.AddMaterialAsync(
            new CreateMaterialDto(form.Name, form.Stock, form.Unit, form.MinStock), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Producto registrado." : "Error al registrar producto.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(AdjustStockForm form, CancellationToken cancellationToken)
    {
        if (!PermissionHelper.CanManageInventory(User))
            return Forbid();

        var result = await _inventoryService.AdjustStockAsync(
            new AdjustStockDto(form.MaterialId, form.NewStock), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Stock actualizado." : "Error al ajustar stock.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int MaterialId, MaterialStatus Status, CancellationToken cancellationToken)
    {
        if (!PermissionHelper.CanManageInventory(User))
            return Forbid();

        var result = await _inventoryService.UpdateStatusAsync(
            new UpdateMaterialStatusDto(MaterialId, Status), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Estado actualizado." : "Error al actualizar estado.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest([Bind(Prefix = "CreateRequest")] CreateRequestForm form, CancellationToken cancellationToken)
    {
        if (!(User.IsInRole(Domain.Entities.UserRoles.Administrador) || User.IsInRole(Domain.Entities.UserRoles.Instructor)))
            return Forbid();

        var result = await _inventoryService.CreateRequestAsync(
            new CreateMaterialRequestDto(form.ProductionOrderId, form.Quantity, form.MaterialId, form.MaterialName), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud creada." : "Error al crear solicitud.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id, CancellationToken cancellationToken)
    {
        if (!PermissionHelper.CanManageInventory(User))
            return Forbid();

        var result = await _inventoryService.ApproveRequestAsync(id, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud aprobada." : "No se pudo aprobar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id, CancellationToken cancellationToken)
    {
        if (!PermissionHelper.CanManageInventory(User))
            return Forbid();

        var result = await _inventoryService.RejectRequestAsync(id, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud rechazada." : "No se pudo rechazar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    private async Task<InventarioIndexViewModel> BuildViewModel(string? filtro, CancellationToken cancellationToken)
    {
        var materials = await _inventoryService.GetMaterialsAsync(cancellationToken);
        var orders = await _orderService.GetOrdersAsync(cancellationToken);
        filtro = string.IsNullOrWhiteSpace(filtro) ? "todos" : filtro.Trim().ToLowerInvariant();

        IEnumerable<MaterialDto> filtered = filtro switch
        {
            "agotados" => materials.Where(m => m.IsDepleted),
            "por-agotarse" => materials.Where(m => !m.IsDepleted && m.IsLowStock),
            "normal" => materials.Where(m => !m.IsLowStock && !m.IsDepleted),
            _ => materials
        };

        return new InventarioIndexViewModel
        {
            Materials = materials,
            FilteredMaterials = filtered
                .OrderBy(m => m.IsDepleted ? 0 : m.IsLowStock ? 1 : 2)
                .ThenBy(m => m.Name)
                .ToList(),
            Filter = filtro,
            DepletedCount = materials.Count(m => m.IsDepleted),
            LowStockCount = materials.Count(m => !m.IsDepleted && m.IsLowStock),
            NormalCount = materials.Count(m => !m.IsLowStock && !m.IsDepleted),
            Requests = await _inventoryService.GetRequestsAsync(cancellationToken),
            Orders = orders,
            CreateMaterial = new CreateMaterialForm(),
            CreateRequest = new CreateRequestForm
            {
                ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0,
                MaterialId = materials.FirstOrDefault()?.Id ?? 0
            },
            CanRegister = PermissionHelper.CanRegisterMaterials(User),
            CanManage = PermissionHelper.CanManageInventory(User),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }
}
