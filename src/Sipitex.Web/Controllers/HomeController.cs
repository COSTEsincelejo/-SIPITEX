using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

public class HomeController : Controller
{
    // Este controlador maneja la página principal y los errores generales del sistema.
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
