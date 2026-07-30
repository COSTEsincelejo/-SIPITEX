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

    // Menú de reportes disponibles
    [HttpGet]
    public IActionResult Index()
    {
        // Título de la pestaña del navegador
        ViewData["Title"] = "Reportes";
        // Migas de pan en el layout
        ViewData["Breadcrumb"] = "SIPITEX / Análisis / Reportes";
        return View();
    }

    // Export inventario: ?format=pdf o excel
    [HttpGet]
    // El servicio genera bytes y yo los devuelvo como archivo
    public async Task<IActionResult> Inventario(string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportInventoryAsync(format, cancellationToken));

    // Export órdenes de producción
    [HttpGet]
    public async Task<IActionResult> Ordenes(string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportOrdersAsync(format, cancellationToken));

    // Export registros de calidad
    [HttpGet]
    public async Task<IActionResult> Calidad(string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportQualityAsync(format, cancellationToken));

    // Export resumen del dashboard
    [HttpGet]
    public async Task<IActionResult> Dashboard(string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportDashboardAsync(format, cancellationToken));

    // Convierte el DTO del servicio en FileContentResult para el navegador
    private FileContentResult FileResult(Application.DTOs.ReportFileDto file) =>
        // Content = bytes, ContentType = application/pdf o excel, FileName para la descarga
        File(file.Content, file.ContentType, file.FileName);
}
