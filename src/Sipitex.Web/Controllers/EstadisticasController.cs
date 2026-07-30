using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Dashboard con números resumidos del taller (producción, inventario, etc.)
[Authorize]
public class EstadisticasController : Controller
{
    private readonly IStatisticsService _statisticsService;

    public EstadisticasController(IStatisticsService statisticsService) => _statisticsService = statisticsService;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new EstadisticasIndexViewModel
        {
            Dashboard = await _statisticsService.GetDashboardAsync(cancellationToken)
        });
    }
}
