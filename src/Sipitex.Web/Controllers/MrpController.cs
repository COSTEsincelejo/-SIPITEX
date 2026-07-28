using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class MrpController : Controller
{
    // Este controlador muestra la información del MRP y la simulación de materiales.
    private readonly IMrpService _mrpService;

    public MrpController(IMrpService mrpService) => _mrpService = mrpService;

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new MrpIndexViewModel
        {
            Bom = await _mrpService.GetBomAsync(cancellationToken),
            Simulation = new MrpSimulationForm()
        });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate(MrpSimulationForm form, CancellationToken cancellationToken)
    {
        return View("Index", new MrpIndexViewModel
        {
            Bom = await _mrpService.GetBomAsync(cancellationToken),
            Simulation = form,
            Result = await _mrpService.SimulateAsync(form.ProductName, form.Quantity, cancellationToken)
        });
    }
}
