using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Descarga de reportes en PDF/Excel según el format del query string
// Gap #11: Instructor → alcance forzado a sí mismo; Inventario bloqueado (gap #12).
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

    // Menú de reportes + filtros (instructor ve solo sus fichas; sin selector de "otro instructor")
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reportes";
        ViewData["Breadcrumb"] = "SIPITEX / Análisis / Reportes";

        var (userId, role, name) = CurrentViewer();
        var isInstructor = IsInstructorOnly();

        var instructors = isInstructor
            ? (await _fichaService.GetActiveInstructorsAsync(cancellationToken))
                .Where(i => i.Id == userId)
                .ToList()
            : await _fichaService.GetActiveInstructorsAsync(cancellationToken);

        var fichas = await _fichaService.GetFichasAsync(userId, role, name, cancellationToken);

        return View(new ReportesIndexViewModel
        {
            Instructors = instructors,
            Fichas = fichas,
            IsInstructorScoped = isInstructor,
            ForcedInstructorId = isInstructor ? userId : null
        });
    }

    // Inventario global: Admin/Bodeguero. Instructor sin acceso (alineado gap #12 / PR #47).
    [HttpGet]
    public async Task<IActionResult> Inventario(
        string format = "pdf",
        int? instructorId = null,
        int? fichaId = null,
        string? jornada = null,
        DateOnly? fecha = null,
        int? mes = null,
        int? anio = null,
        CancellationToken cancellationToken = default)
    {
        if (IsInstructorOnly())
            return Forbid();

        return FileResult(await _reportService.ExportInventoryAsync(
            format, ToFilter(instructorId, fichaId, jornada, fecha, mes, anio), cancellationToken));
    }

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
            format,
            ResolveFilter(instructorId, fichaId, jornada, fecha, mes, anio),
            cancellationToken));

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
            format,
            ResolveFilter(instructorId, fichaId, jornada, fecha, mes, anio),
            cancellationToken));

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
            format,
            ResolveFilter(instructorId, fichaId, jornada, fecha, mes, anio),
            cancellationToken));

    // Actividad del instructor: Instructor forzado a sí mismo; Admin/Bodeguero eligen en query
    [HttpGet]
    public async Task<IActionResult> ActividadInstructor(
        string format = "pdf",
        int? instructorId = null,
        int? fichaId = null,
        string? jornada = null,
        DateOnly? fecha = null,
        int? mes = null,
        int? anio = null,
        CancellationToken cancellationToken = default)
    {
        var filter = ResolveFilter(instructorId, fichaId, jornada, fecha, mes, anio)
                     ?? new ReportFilterDto();
        // Este reporte exige InstructorId; con Instructor el ResolveFilter ya lo fuerza
        return FileResult(await _reportService.ExportActividadInstructorAsync(format, filter, cancellationToken));
    }

    // Instructor: siempre InstructorId = NameIdentifier (ignora query). Admin/Bodeguero: query intacta.
    private ReportFilterDto? ResolveFilter(
        int? instructorId,
        int? fichaId,
        string? jornada,
        DateOnly? fecha,
        int? mes,
        int? anio)
    {
        if (IsInstructorOnly() && TryGetUserId(out var selfId))
            instructorId = selfId;

        return ToFilter(instructorId, fichaId, jornada, fecha, mes, anio);
    }

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

    // Instructor sin ser también Admin (por si hubiera claims multi-rol)
    private bool IsInstructorOnly() =>
        User.IsInRole(UserRoles.Instructor) && !User.IsInRole(UserRoles.Administrador);

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0)
            userId = id;

        return (userId, User.FindFirstValue(ClaimTypes.Role), User.FindFirstValue(ClaimTypes.Name));
    }
}
