using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Página de inicio y la vista genérica de error
public class HomeController : Controller
{
    [Authorize]
    public IActionResult Index()
    {
        ViewData["Title"] = "Inicio";
        ViewData["Breadcrumb"] = "SIPITEX / Inicio";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // Sin caché para que el error muestre el RequestId actual
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
