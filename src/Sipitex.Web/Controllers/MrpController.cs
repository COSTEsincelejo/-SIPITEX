using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// BOM y simulación de materiales necesarios para producir
[Authorize]
public class MrpController : Controller
{
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

    [Authorize(Policy = AuthorizationPolicyNames.PuedeSimularMrp)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate([Bind(Prefix = "Simulation")] MrpSimulationForm form, CancellationToken cancellationToken)
    {
        // Misma vista con el resultado debajo del formulario
        return View("Index", new MrpIndexViewModel
        {
            Bom = await _mrpService.GetBomAsync(cancellationToken),
            Simulation = form,
            Result = await _mrpService.SimulateAsync(form.ProductName, form.Quantity, cancellationToken)
        });
    }
}
