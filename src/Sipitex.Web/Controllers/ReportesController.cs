using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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
    private const int MaximoDiasActividad = 92;

    private readonly IReportService _reportService;
    private readonly IFichaService _fichaService;
    private readonly IActividadInstructorService _actividad;
    private readonly IActividadInstructorPdf _actividadPdf;
    private readonly IActividadInstructorExcel _actividadExcel;

    public ReportesController(
        IReportService reportService,
        IFichaService fichaService,
        IActividadInstructorService actividad,
        IActividadInstructorPdf actividadPdf,
        IActividadInstructorExcel actividadExcel)
    {
        _reportService = reportService;
        _fichaService = fichaService;
        _actividad = actividad;
        _actividadPdf = actividadPdf;
        _actividadExcel = actividadExcel;
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

    // Inventario global: Admin/EncargadoDeBodega. Instructor sin acceso (alineado gap #12 / PR #47).
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

    // Enlace viejo de la tarjeta de reportes. Un día o un mes se traducen al mismo
    // rango que usa la pantalla; el archivo sale de ActividadInstructorService.
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
        if (filter.InstructorId is not int id || id <= 0 || !TryRangoActividad(filter, out var desde, out var hasta))
        {
            return RedirectToAction(nameof(Actividad), new
            {
                instructorId = filter.InstructorId,
                fichaId = filter.FichaId,
                jornada = filter.Jornada
            });
        }

        return await ExportarActividadAsync(format, id, desde, hasta, filter.FichaId, filter.Jornada, tipo: null, cancellationToken);
    }

    // Resumen visual. El PDF y el Excel salen de esta misma consulta.
    [HttpGet]
    public async Task<IActionResult> Actividad(
        int? instructorId = null,
        DateOnly? desde = null,
        DateOnly? hasta = null,
        int? fichaId = null,
        string? jornada = null,
        string? tipo = null,
        CancellationToken cancellationToken = default)
    {
        var (model, reporte) = await ConsultarPantallaAsync(
            instructorId, desde, hasta, fichaId, jornada, tipo, cancellationToken);
        model.Reporte = reporte;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ActividadPdf(
        int? instructorId = null,
        DateOnly? desde = null,
        DateOnly? hasta = null,
        int? fichaId = null,
        string? jornada = null,
        string? tipo = null,
        CancellationToken cancellationToken = default)
    {
        var (model, reporte) = await ConsultarPantallaAsync(
            instructorId, desde, hasta, fichaId, jornada, tipo, cancellationToken);
        if (reporte is null)
        {
            model.Consultado = true;
            model.Error ??= "Indique el instructor y el período para generar el PDF.";
            return View("Actividad", model);
        }

        return FileResult(_actividadPdf.Render(reporte));
    }

    [HttpGet]
    public async Task<IActionResult> ActividadExcel(
        int? instructorId = null,
        DateOnly? desde = null,
        DateOnly? hasta = null,
        int? fichaId = null,
        string? jornada = null,
        string? tipo = null,
        CancellationToken cancellationToken = default)
    {
        var (model, reporte) = await ConsultarPantallaAsync(
            instructorId, desde, hasta, fichaId, jornada, tipo, cancellationToken);
        if (reporte is null)
        {
            model.Consultado = true;
            model.Error ??= "Indique el instructor y el período para generar el Excel.";
            return View("Actividad", model);
        }

        return FileResult(_actividadExcel.Render(reporte));
    }

    private async Task<IActionResult> ExportarActividadAsync(
        string format,
        int instructorId,
        DateOnly desde,
        DateOnly hasta,
        int? fichaId,
        string? jornada,
        string? tipo,
        CancellationToken cancellationToken)
    {
        if (hasta.DayNumber - desde.DayNumber + 1 > MaximoDiasActividad)
        {
            return RedirectToAction(nameof(Actividad), new
            {
                instructorId,
                desde,
                hasta,
                fichaId,
                jornada,
                tipo
            });
        }

        var reporte = await _actividad.ConsultarAsync(
            new ActividadInstructorConsulta(instructorId, desde, hasta, fichaId, jornada, tipo),
            cancellationToken);
        var excel = format.Equals("excel", StringComparison.OrdinalIgnoreCase)
                    || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase);
        return FileResult(excel ? _actividadExcel.Render(reporte) : _actividadPdf.Render(reporte));
    }

    private async Task<(ActividadInstructorViewModel Model, ActividadInstructorReporteDto? Reporte)> ConsultarPantallaAsync(
        int? instructorId,
        DateOnly? desde,
        DateOnly? hasta,
        int? fichaId,
        string? jornada,
        string? tipo,
        CancellationToken cancellationToken)
    {
        var model = await ArmarActividadAsync(instructorId, desde, hasta, fichaId, jornada, tipo, cancellationToken);
        if (!model.Consultado || model.InstructorId is not int id || model.Desde is not DateOnly d || model.Hasta is not DateOnly h)
            return (model, null);

        var reporte = await _actividad.ConsultarAsync(
            new ActividadInstructorConsulta(id, d, h, model.FichaId, model.Jornada, model.Tipo),
            cancellationToken);
        if (!reporte.InstructorExiste)
        {
            model.Error = "No se encontró ese instructor.";
            return (model, null);
        }

        return (model, reporte);
    }

    // Fecha suelta = ese día. Mes y año = el mes completo. Sin eso, la pantalla pide el rango.
    private static bool TryRangoActividad(ReportFilterDto filter, out DateOnly desde, out DateOnly hasta)
    {
        if (filter.Fecha is DateOnly dia)
        {
            desde = hasta = dia;
            return true;
        }

        if (filter.Mes is int mes and >= 1 and <= 12 && filter.Anio is int anio and >= 2000 and <= 2100)
        {
            desde = new DateOnly(anio, mes, 1);
            hasta = desde.AddMonths(1).AddDays(-1);
            return true;
        }

        desde = default;
        hasta = default;
        return false;
    }

    private async Task<ActividadInstructorViewModel> ArmarActividadAsync(
        int? instructorId,
        DateOnly? desde,
        DateOnly? hasta,
        int? fichaId,
        string? jornada,
        string? tipo,
        CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var isInstructor = IsInstructorOnly();
        if (isInstructor && userId is int self)
            instructorId = self;

        var instructors = isInstructor
            ? (await _fichaService.GetActiveInstructorsAsync(cancellationToken)).Where(i => i.Id == userId).ToList()
            : await _fichaService.GetActiveInstructorsAsync(cancellationToken);
        var fichas = await _fichaService.GetFichasAsync(userId, role, name, cancellationToken);
        var elegido = instructors.FirstOrDefault(i => i.Id == instructorId);
        var intento = instructorId is > 0 || desde is not null || hasta is not null
            || fichaId is > 0 || !string.IsNullOrWhiteSpace(jornada) || !string.IsNullOrWhiteSpace(tipo);

        string? error = null;
        var consultado = false;
        if (intento)
        {
            consultado = true;
            if (instructorId is not > 0 || elegido is null)
                error = "Seleccione un instructor de la lista.";
            else if (desde is null || hasta is null)
                error = "Indique el período Desde y Hasta.";
            else if (desde > hasta)
                error = "La fecha Desde no puede ser posterior a Hasta.";
            else if (hasta.Value.DayNumber - desde.Value.DayNumber + 1 > MaximoDiasActividad)
                error = $"El período máximo es de {MaximoDiasActividad} días. Para un mes use Este mes o Mes anterior.";
            else if (fichaId is > 0 && fichas.All(f => f.Id != fichaId))
                error = "La ficha no está en su alcance.";
        }

        if (error is not null)
            consultado = true;

        var json = JsonSerializer.Serialize(
            instructors.Select(i => new { id = i.Id, nombre = i.Nombre, meta = "Instructor" }),
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.Default });

        return new ActividadInstructorViewModel
        {
            Instructors = instructors,
            Fichas = fichas,
            InstructoresJson = json,
            IsInstructorScoped = isInstructor,
            InstructorId = elegido?.Id,
            InstructorNombre = elegido?.Nombre ?? string.Empty,
            Desde = desde,
            Hasta = hasta,
            FichaId = fichaId is > 0 ? fichaId : null,
            Jornada = string.IsNullOrWhiteSpace(jornada) ? null : jornada.Trim(),
            Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim(),
            Consultado = consultado && error is null,
            Error = error
        };
    }

    // Instructor: siempre InstructorId = NameIdentifier (ignora query). Admin/EncargadoDeBodega: query intacta.
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
