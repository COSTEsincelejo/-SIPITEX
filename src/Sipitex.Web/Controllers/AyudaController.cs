using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sipitex.Application.Ayuda;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Centro de ayuda. El texto vive en AyudaCatalogo; aquí solo se arma la página y los PDF que existan.
[Authorize]
public class AyudaController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly SoporteOptions _soporte;

    public AyudaController(IWebHostEnvironment env, IOptions<SoporteOptions> soporte)
    {
        _env = env;
        _soporte = soporte.Value;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Ayuda";
        ViewData["Breadcrumb"] = "SIPITEX / Ayuda";
        return View(new AyudaIndexViewModel
        {
            Temas = AyudaCatalogo.Visibles(User),
            Glosario = AyudaCatalogo.Glosario,
            Responsable = string.IsNullOrWhiteSpace(_soporte.Responsable)
                ? "Coordinación del sistema"
                : _soporte.Responsable.Trim(),
            Correo = (_soporte.Correo ?? "").Trim(),
            Manuales = BuscarManuales()
        });
    }

    // Solo se enlazan PDF reales en wwwroot/manuales:
    // manual-administrador.pdf, manual-instructor.pdf, manual-encargado.pdf.
    private IReadOnlyList<ManualAyudaItem> BuscarManuales()
    {
        var dir = Path.Combine(_env.WebRootPath, "manuales");
        var defs = new (string Archivo, string Titulo, string Rol)[]
        {
            ("manual-administrador.pdf", "Manual del administrador", UserRoles.Administrador),
            ("manual-instructor.pdf", "Manual del instructor", UserRoles.Instructor),
            ("manual-encargado.pdf", "Manual del encargado de bodega", UserRoles.EncargadoDeBodega)
        };

        var lista = new List<ManualAyudaItem>();
        foreach (var def in defs)
        {
            var esAdmin = User.IsInRole(UserRoles.Administrador);
            if (!esAdmin && !User.IsInRole(def.Rol))
                continue;

            var ruta = Path.Combine(dir, def.Archivo);
            if (!System.IO.File.Exists(ruta))
                continue;

            lista.Add(new ManualAyudaItem(def.Titulo, "/manuales/" + def.Archivo));
        }

        return lista;
    }
}
