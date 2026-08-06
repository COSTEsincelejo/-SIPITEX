using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Descarga de reportes en PDF/Excel según el format del query string
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

    // Menú de reportes + filtros opcionales
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reportes";
        ViewData["Breadcrumb"] = "SIPITEX / Análisis / Reportes";

        var instructors = await _fichaService.GetActiveInstructorsAsync(cancellationToken);
        var fichas = await _fichaService.GetFichasAsync(cancellationToken: cancellationToken);

        return View(new ReportesIndexViewModel
        {
            Instructors = instructors,
            Fichas = fichas
        });
    }

    // Export inventario: ?format=pdf|excel + filtros opcionales
    [HttpGet]
    public async Task<IActionResult> Inventario(
        string format = "pdf",
        int? instructorId = null,
        int? fichaId = null,
        string? jornada = null,
        DateOnly? fecha = null,
        int? mes = null,
        int? anio = null,
        CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportInventoryAsync(
            format, ToFilter(instructorId, fichaId, jornada, fecha, mes, anio), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Ordenes(
        string format = "pdf",
        int? instructorId = null,
        int? fichaId = null,
        string? jornada = null,
        DateOnly? fecha = null,
        int? mes = null,
        int? anio = null,
        CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportOrdersAsync(
            format, ToFilter(instructorId, fichaId, jornada, fecha, mes, anio), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Calidad(
        string format = "pdf",
        int? instructorId = null,
        int? fichaId = null,
        string? jornada = null,
        DateOnly? fecha = null,
        int? mes = null,
        int? anio = null,
        CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportQualityAsync(
            format, ToFilter(instructorId, fichaId, jornada, fecha, mes, anio), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Dashboard(
        string format = "pdf",
        int? instructorId = null,
        int? fichaId = null,
        string? jornada = null,
        DateOnly? fecha = null,
        int? mes = null,
        int? anio = null,
        CancellationToken cancellationToken = default) =>
        FileResult(await _reportService.ExportDashboardAsync(
            format, ToFilter(instructorId, fichaId, jornada, fecha, mes, anio), cancellationToken));

    private static ReportFilterDto? ToFilter(
        int? instructorId,
        int? fichaId,
        string? jornada,
        DateOnly? fecha,
        int? mes,
        int? anio)
    {
        var filter = new ReportFilterDto(instructorId, fichaId, jornada, fecha, mes, anio);
        return filter.HasAny ? filter : null;
    }

    private FileContentResult FileResult(ReportFileDto file) =>
        File(file.Content, file.ContentType, file.FileName);
}
