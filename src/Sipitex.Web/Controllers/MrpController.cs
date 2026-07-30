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

    // Servicio que tiene el BOM y la lógica de simulación
    public MrpController(IMrpService mrpService) => _mrpService = mrpService;

    // Muestra la lista BOM y el formulario de simulación vacío
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new MrpIndexViewModel
        {
            // Tabla con todos los materiales por producto
            Bom = await _mrpService.GetBomAsync(cancellationToken),
            // Form vacío para pedir producto y cantidad
            Simulation = new MrpSimulationForm()
        });
    }

    // Corre la simulación MRP con producto y cantidad del form
    [Authorize(Policy = AuthorizationPolicyNames.PuedeSimularMrp)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate([Bind(Prefix = "Simulation")] MrpSimulationForm form, CancellationToken cancellationToken)
    {
        // Misma vista con el resultado debajo del formulario
        return View("Index", new MrpIndexViewModel
        {
            // Vuelvo a cargar el BOM para la tabla
            Bom = await _mrpService.GetBomAsync(cancellationToken),
            // Conservo lo que el usuario escribió en el form
            Simulation = form,
            // Líneas con requerido, disponible y déficit por material
            Result = await _mrpService.SimulateAsync(form.ProductName, form.Quantity, cancellationToken)
        });
    }
}
