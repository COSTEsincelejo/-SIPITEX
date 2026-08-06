using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Helpers;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class MrpController : Controller
{
    private readonly IMrpService _mrpService;
    private readonly IInventoryService _inventoryService;

    public MrpController(IMrpService mrpService, IInventoryService inventoryService)
    {
        _mrpService = mrpService;
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await BuildViewModel(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate([Bind(Prefix = "Simulation")] MrpSimulationForm form, CancellationToken cancellationToken)
    {
        var vm = await BuildViewModel(cancellationToken);
        vm.Simulation = form;
        vm.Result = await _mrpService.SimulateAsync(form.ProductName, form.Quantity, cancellationToken);
        if (vm.Result.Lines.Count == 0)
        {
            vm.Message = $"«{form.ProductName}» aún no tiene ficha BOM. Agréguele materiales abajo o simule otro producto.";
            vm.IsSuccess = false;
        }
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBomItem([Bind(Prefix = "AddBom")] AddBomItemForm form, CancellationToken cancellationToken)
    {
        if (!PermissionHelper.CanManageInventory(User))
            return Forbid();

        var result = await _mrpService.AddBomItemAsync(form.ProductName, form.MaterialName, form.QuantityPerUnit, form.Unit, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    private async Task<MrpIndexViewModel> BuildViewModel(CancellationToken cancellationToken)
    {
        var products = await _mrpService.GetKnownProductNamesAsync(cancellationToken);
        var materials = await _inventoryService.GetMaterialsAsync(cancellationToken);
        return new MrpIndexViewModel
        {
            Bom = await _mrpService.GetBomAsync(cancellationToken),
            KnownProducts = products,
            Materials = materials,
            Simulation = new MrpSimulationForm
            {
                ProductName = products.FirstOrDefault() ?? string.Empty
            },
            AddBom = new AddBomItemForm
            {
                ProductName = products.FirstOrDefault() ?? string.Empty
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }
}
