using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Web.Controllers;

// Descarga de reportes en PDF/Excel según el format del query string
[Authorize]
public class ReportesController : Controller
{
    private readonly IReportService _reportService;

    public ReportesController(IReportService reportService) => _reportService = reportService;

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Reportes";
        ViewData["Breadcrumb"] = "SIPITEX / Análisis / Reportes";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Inventario(string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportInventoryAsync(format, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Ordenes(string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportOrdersAsync(format, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Calidad(string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportQualityAsync(format, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Dashboard(string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportDashboardAsync(format, cancellationToken));

    // Convierte el DTO del servicio en FileContentResult para el navegador
    private FileContentResult FileResult(Application.DTOs.ReportFileDto file) =>
        File(file.Content, file.ContentType, file.FileName);
}
