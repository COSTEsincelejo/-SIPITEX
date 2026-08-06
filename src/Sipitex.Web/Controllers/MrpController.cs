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
        var products = await _bomCatalog.GetProductsAsync(cancellationToken);
        return View(new MrpIndexViewModel
        {
            Bom = await _mrpService.GetBomAsync(cancellationToken),
            Products = products,
            ProductNames = products.Select(p => p.ProductName).ToList(),
            Simulation = new MrpSimulationForm
            {
                ProductName = products.FirstOrDefault()?.ProductName ?? string.Empty
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeSimularMrp)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate([Bind(Prefix = "Simulation")] MrpSimulationForm form, CancellationToken cancellationToken)
    {
        var products = await _bomCatalog.GetProductsAsync(cancellationToken);
        return View("Index", new MrpIndexViewModel
        {
            Bom = await _mrpService.GetBomAsync(cancellationToken),
            Products = products,
            ProductNames = products.Select(p => p.ProductName).ToList(),
            Simulation = form,
            Result = await _mrpService.SimulateAsync(form.ProductName, form.Quantity, cancellationToken)
        });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View("Edit", await BuildEditVm(null, cancellationToken));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
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

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var detail = await _bomCatalog.GetProductAsync(id, cancellationToken);
        if (detail is null) return NotFound();
        return View(await BuildEditVm(detail, cancellationToken));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
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
