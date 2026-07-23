using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class ReportesController : Controller
{
    private readonly IReportService _reportService;

    public ReportesController(IReportService reportService) => _reportService = reportService;

    [HttpGet]
    public IActionResult Index(int? year, int? month)
    {
        ViewData["Title"] = "Reportes";
        ViewData["Breadcrumb"] = "SIPITEX / Análisis / Reportes";
        var today = DateTime.Today;
        return View(new ReportesIndexViewModel
        {
            Year = year ?? today.Year,
            Month = month ?? today.Month
        });
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

    [HttpGet]
    public async Task<IActionResult> Mensual(int year, int month, string format = "pdf", CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportMonthlyAsync(year, month, format, cancellationToken));

    private FileContentResult FileResult(Application.DTOs.ReportFileDto file) =>
        File(file.Content, file.ContentType, file.FileName);
}
