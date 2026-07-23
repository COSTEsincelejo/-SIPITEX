using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class ReportesController : Controller
{
    private readonly IReportService _reportService;
    private readonly IFichaService _fichaService;

    public ReportesController(IReportService reportService, IFichaService fichaService)
    {
        _reportService = reportService;
        _fichaService = fichaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reportes";
        ViewData["Breadcrumb"] = "SIPITEX / Análisis / Reportes";
        var today = DateTime.Today;
        var fichas = await _fichaService.GetFichasAsync(cancellationToken);
        return View(new ReportesIndexViewModel
        {
            Year = today.Year,
            Month = today.Month,
            Date = DateOnly.FromDateTime(today),
            Period = "mes",
            Fichas = fichas,
            Instructors = fichas
                .Select(f => f.InstructorName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList()
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

    [HttpGet]
    public async Task<IActionResult> Filtrado(
        string period = "mes",
        DateOnly? date = null,
        int? year = null,
        int? month = null,
        string? instructor = null,
        int? fichaId = null,
        string format = "pdf",
        CancellationToken cancellationToken = default)
    {
        var filter = new ReportFilterDto(
            period,
            date,
            year,
            month,
            string.IsNullOrWhiteSpace(instructor) ? null : instructor,
            fichaId is > 0 ? fichaId : null,
            format);
        return FileResult(await _reportService.ExportFilteredAsync(filter, cancellationToken));
    }

    private FileContentResult FileResult(ReportFileDto file) =>
        File(file.Content, file.ContentType, file.FileName);
}
