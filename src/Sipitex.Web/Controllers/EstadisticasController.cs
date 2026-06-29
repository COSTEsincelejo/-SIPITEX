using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

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
