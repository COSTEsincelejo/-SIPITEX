using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

public class FichasController : Controller
{
    private readonly IFichaService _fichaService;
    private readonly IProductionOrderService _orderService;

    public FichasController(IFichaService fichaService, IProductionOrderService orderService)
    {
        _fichaService = fichaService;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersAsync(cancellationToken);
        var fichas = await _fichaService.GetFichasAsync(cancellationToken);

        return View(new FichasIndexViewModel
        {
            Fichas = fichas,
            Orders = orders,
            Register = new RegisterProductionForm
            {
                ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0,
                FichaId = fichas.FirstOrDefault()?.Id ?? 0
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterProductionForm form, CancellationToken cancellationToken)
    {
        var result = await _fichaService.RegisterSessionAsync(
            new RegisterProductionDto(form.ProductionOrderId, form.FichaId, form.Units), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Sesión registrada." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickRegister(int fichaId, int units, CancellationToken cancellationToken)
    {
        var result = await _fichaService.QuickRegisterAsync(fichaId, units, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Registro exitoso." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }
}
