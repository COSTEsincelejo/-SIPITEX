using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class FichasController : Controller
{
    // Aquí se administran las fichas y el registro de producción por ficha.
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
            Sessions = await _fichaService.GetRecentSessionsAsync(cancellationToken),
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
            new RegisterProductionDto(form.ProductionOrderId, form.FichaId, form.Units, form.Observations), cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Sesión registrada." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickRegister(int fichaId, int units, string? observations, CancellationToken cancellationToken)
    {
        var result = await _fichaService.QuickRegisterAsync(fichaId, units, observations, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Registro exitoso." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }
}
