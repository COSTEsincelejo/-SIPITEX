using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Dashboard con números resumidos del taller (producción, inventario, etc.)
[Authorize]
public class EstadisticasController : Controller
{
    // Servicio que junta los números del taller
    private readonly IStatisticsService _statisticsService;

    public EstadisticasController(IStatisticsService statisticsService) => _statisticsService = statisticsService;

    // Trae los KPIs del servicio y los manda a la vista
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Armo el view model solo con el dashboard
        return View(new EstadisticasIndexViewModel
        {
            Dashboard = await _statisticsService.GetDashboardAsync(cancellationToken)
        });
    }
}
