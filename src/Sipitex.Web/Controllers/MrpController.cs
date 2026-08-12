using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// BOM, fichas técnicas y simulación MRP
[Authorize]
public class MrpController : Controller
{
    private readonly IMrpService _mrpService;
    private readonly IBomCatalogService _bomCatalog;
    private readonly IInventoryService _inventoryService;

    public MrpController(IMrpService mrpService, IBomCatalogService bomCatalog, IInventoryService inventoryService)
    {
        _mrpService = mrpService;
        _bomCatalog = bomCatalog;
        _inventoryService = inventoryService;
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await BuildIndexVm(cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeSimularMrp)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate([Bind(Prefix = "Simulation")] MrpSimulationForm form, CancellationToken cancellationToken)
    {
        var products = await GetScopedProductsAsync(cancellationToken);
        var allowedNames = products.Select(p => p.ProductName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!allowedNames.Contains(form.ProductName))
        {
            var denied = await BuildIndexVm(cancellationToken, form);
            denied.Message = "No puede simular una ficha técnica que no tiene asignada.";
            denied.IsSuccess = false;
            return View("Index", denied);
        }

        var vm = await BuildIndexVm(cancellationToken, form);
        vm.Result = await _mrpService.SimulateAsync(form.ProductName, form.Quantity, cancellationToken);
        return View("Index", vm);
    }

    // Asignar instructor a ficha técnica (solo Administrador) — gap #4
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignInstructor(int bomProductId, int instructorUserId, CancellationToken cancellationToken)
    {
        var result = await _bomCatalog.AssignInstructorAsync(bomProductId, instructorUserId, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Instructor asignado." : "No se pudo asignar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Quitar instructor de ficha técnica (solo Administrador) — gap #4
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveInstructor(int bomProductId, int instructorUserId, CancellationToken cancellationToken)
    {
        var result = await _bomCatalog.RemoveInstructorAsync(bomProductId, instructorUserId, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Instructor quitado." : "No se pudo quitar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeGestionarFichasTecnicas)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View("Edit", await BuildEditVm(null, cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeGestionarFichasTecnicas)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] BomProductEditForm form, CancellationToken cancellationToken)
    {
        var result = await _bomCatalog.CreateAsync(MapForm(form), cancellationToken);
        if (!result.Success)
        {
            var vm = await BuildEditVm(null, cancellationToken, form);
            vm.Message = result.Message;
            vm.IsSuccess = false;
            return View("Edit", vm);
        }

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeGestionarFichasTecnicas)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var detail = await _bomCatalog.GetProductAsync(id, cancellationToken);
        if (detail is null) return NotFound();
        return View(await BuildEditVm(detail, cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeGestionarFichasTecnicas)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Form")] BomProductEditForm form, CancellationToken cancellationToken)
    {
        var result = await _bomCatalog.UpdateAsync(id, MapForm(form), cancellationToken);
        if (!result.Success)
        {
            var detail = await _bomCatalog.GetProductAsync(id, cancellationToken);
            var vm = await BuildEditVm(detail, cancellationToken, form);
            vm.Message = result.Message;
            vm.IsSuccess = false;
            return View(vm);
        }

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }

    // Delete permanece solo Administrador (conservador; el permiso extendido no lo habilita)
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _bomCatalog.DeleteAsync(id, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    private async Task<MrpIndexViewModel> BuildIndexVm(
        CancellationToken cancellationToken,
        MrpSimulationForm? simulation = null)
    {
        var products = await GetScopedProductsAsync(cancellationToken);
        var productNames = products.Select(p => p.ProductName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allBom = await _mrpService.GetBomAsync(cancellationToken);
        // Instructor sin permiso de gestión: solo líneas BOM de fichas asignadas
        var bom = IsConsultaInstructorScoped()
            ? allBom.Where(b => productNames.Contains(b.ProductName)).ToList()
            : allBom.ToList();

        var instructors = User.IsInRole(UserRoles.Administrador)
            ? await _bomCatalog.GetAssignableInstructorsAsync(cancellationToken)
            : Array.Empty<InstructorOptionDto>();

        return new MrpIndexViewModel
        {
            Bom = bom,
            Products = products,
            ProductNames = products.Select(p => p.ProductName).ToList(),
            Instructors = instructors,
            IsAdministrator = User.IsInRole(UserRoles.Administrador),
            Simulation = simulation ?? new MrpSimulationForm
            {
                ProductName = products.FirstOrDefault()?.ProductName ?? string.Empty
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }

    private async Task<IReadOnlyList<BomProductListItemDto>> GetScopedProductsAsync(CancellationToken cancellationToken)
    {
        // Instructor con Mrp.GestionarFichas (o Admin/Bodeguero): ve todas.
        // Instructor solo consulta: solo fichas técnicas asignadas (gap #4).
        if (IsConsultaInstructorScoped()
            && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var instructorId))
        {
            return await _bomCatalog.GetProductsAsync(instructorId, cancellationToken);
        }

        return await _bomCatalog.GetProductsAsync(cancellationToken: cancellationToken);
    }

    private bool IsConsultaInstructorScoped() =>
        User.IsInRole(UserRoles.Instructor)
        && !User.IsInRole(UserRoles.Administrador)
        && !User.IsInRole(UserRoles.Bodeguero)
        && !PermissionRules.PuedeGestionarFichasTecnicas(User);

    private async Task<BomProductEditViewModel> BuildEditVm(
        BomProductDetailDto? detail,
        CancellationToken cancellationToken,
        BomProductEditForm? form = null)
    {
        var materials = await _inventoryService.GetMaterialsAsync(cancellationToken);
        form ??= detail is null
            ? new BomProductEditForm
            {
                HabilitadoParaOrdenes = true,
                Lines = [new BomRecipeLineForm()]
            }
            : new BomProductEditForm
            {
                Id = detail.Id,
                ProductName = detail.ProductName,
                IsReference = detail.IsReference,
                Notes = detail.Notes,
                HabilitadoParaOrdenes = detail.HabilitadoParaOrdenes,
                Lines = detail.Lines.Select(l => new BomRecipeLineForm
                {
                    ItemId = l.ItemId,
                    MaterialId = l.MaterialId,
                    QuantityPerUnit = l.QuantityPerUnit,
                    Unit = l.Unit
                }).ToList()
            };

        return new BomProductEditViewModel
        {
            Form = form,
            Materials = materials,
            IsEdit = detail is not null || form.Id > 0
        };
    }

    private static UpsertBomProductDto MapForm(BomProductEditForm form) =>
        new(
            form.ProductName,
            form.IsReference,
            form.Notes,
            form.HabilitadoParaOrdenes,
            form.Lines.Select(l => new BomRecipeLineDto(
                l.ItemId,
                l.MaterialId > 0 ? l.MaterialId : null,
                l.NewMaterialName,
                l.NewMaterialUnit,
                l.QuantityPerUnit,
                l.Unit)).ToList());
}
