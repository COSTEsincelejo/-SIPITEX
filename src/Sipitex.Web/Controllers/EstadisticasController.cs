using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Dashboard con números resumidos del taller (producción, inventario, etc.)
// Instructor: KPIs acotados a sus órdenes (mismo patrón que ReportesController.ResolveFilter).
[Authorize]
public class EstadisticasController : Controller
{
    private readonly IStatisticsService _statisticsService;

    public EstadisticasController(IStatisticsService statisticsService) => _statisticsService = statisticsService;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        // Instructor: fuerza alcance self (equivalente a ResolveFilter instructorId=self).
        // Admin/Bodeguero: sin restricción (GetOrdersAsync no filtra esos roles).
        return View(new EstadisticasIndexViewModel
        {
            Dashboard = await _statisticsService.GetDashboardAsync(
                userId, role, name, cancellationToken)
        });
    }

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0)
            userId = id;

        return (userId, User.FindFirstValue(ClaimTypes.Role), User.FindFirstValue(ClaimTypes.Name));
    }
}
