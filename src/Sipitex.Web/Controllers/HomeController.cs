using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Página de inicio y la vista genérica de error
public class HomeController : Controller
{
    // Landing después de entrar (necesita estar logueado)
    [Authorize]
    public IActionResult Index()
    {
        ViewData["Title"] = "Inicio";
        ViewData["Breadcrumb"] = "SIPITEX / Inicio";
        return View();
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
