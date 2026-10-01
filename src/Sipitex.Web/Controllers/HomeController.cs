using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Página de inicio y la vista genérica de error
public class HomeController : Controller
{
    private readonly IStatisticsService _statisticsService;

    public HomeController(IStatisticsService statisticsService) => _statisticsService = statisticsService;

    // Landing después de entrar (necesita estar logueado)
    [Authorize]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Inicio";
        ViewData["Breadcrumb"] = "SIPITEX / Inicio";
        var (userId, role, name) = CurrentViewer();
        var dashboard = await _statisticsService.GetDashboardAsync(userId, role, name, cancellationToken);
        return View(dashboard);
    }

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0)
            userId = id;

        return (userId, User.FindFirstValue(ClaimTypes.Role), User.FindFirstValue(ClaimTypes.Name));
    }

    // Página de política de privacidad (pública)
    public IActionResult Privacy()
    {
        return View();
    }

    // Sin caché para que el error muestre el RequestId actual
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Activity.Current es el trace de .NET; si no hay, uso el id del request HTTP
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
