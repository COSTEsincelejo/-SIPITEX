using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Actas de verificación: observación del instructor + checklist + firma
[Authorize]
public class ActasVerificacionController : Controller
{
    private readonly IActaVerificacionService _actaService;
    private readonly IProductionOrderService _orderService;
    private readonly IFichaService _fichaService;

    public ActasVerificacionController(
        IActaVerificacionService actaService,
        IProductionOrderService orderService,
        IFichaService fichaService)
    {
        _actaService = actaService;
        _orderService = orderService;
        _fichaService = fichaService;
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var isInstructor = string.Equals(role, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);

        var orders = await _orderService.GetOrdersAsync(cancellationToken);
        var fichas = await _fichaService.GetFichasAsync(userId, role, name, cancellationToken);

        return View(new ActasVerificacionIndexViewModel
        {
            Actas = await _actaService.GetActasAsync(userId, role, name, cancellationToken),
            Orders = orders,
            Fichas = fichas,
            PuedeCrear = isInstructor,
            Create = new CreateActaVerificacionForm
            {
                ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0,
                FichaId = fichas.FirstOrDefault()?.Id ?? 0
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [Authorize(Roles = UserRoles.Instructor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Create")] CreateActaVerificacionForm form,
        CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        if (userId is null)
            return Challenge();

        var result = await _actaService.CreateAsync(
            ToDto(form),
            userId.Value,
            role,
            name,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Acta creada." : "No se pudo crear el acta.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var acta = await _actaService.GetByIdAsync(id, userId, role, name, cancellationToken);
        if (acta is null) return NotFound();

        return View(new ActaVerificacionDetailViewModel
        {
            Acta = acta,
            Edit = new EditActaVerificacionForm
            {
                Observacion = acta.Observacion,
                CumpleEspecificaciones = acta.CumpleEspecificaciones,
                CumpleAcabados = acta.CumpleAcabados,
                CumpleSinDefectos = acta.CumpleSinDefectos,
                ChecklistCumpleRequisitos = acta.ChecklistCumpleRequisitos
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [Authorize(Roles = UserRoles.Instructor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(int id, [Bind(Prefix = "Edit")] EditActaVerificacionForm form, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        if (userId is null)
            return Challenge();

        var existing = await _actaService.GetByIdAsync(id, userId, role, name, cancellationToken);
        if (existing is null) return NotFound();

        var result = await _actaService.UpdateAsync(
            id,
            new GuardarActaVerificacionDto(
                existing.ProductionOrderId,
                existing.FichaId,
                form.Observacion,
                form.CumpleEspecificaciones,
                form.CumpleAcabados,
                form.CumpleSinDefectos,
                form.ChecklistCumpleRequisitos),
            userId.Value,
            role,
            name,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Guardado." : "No se pudo guardar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Roles = UserRoles.Instructor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Firmar(int id, [Bind(Prefix = "Edit")] EditActaVerificacionForm form, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        if (userId is null)
            return Challenge();

        var existing = await _actaService.GetByIdAsync(id, userId, role, name, cancellationToken);
        if (existing is null) return NotFound();

        // Persiste checklist/observación actuales antes de intentar firmar
        var update = await _actaService.UpdateAsync(
            id,
            new GuardarActaVerificacionDto(
                existing.ProductionOrderId,
                existing.FichaId,
                form.Observacion,
                form.CumpleEspecificaciones,
                form.CumpleAcabados,
                form.CumpleSinDefectos,
                form.ChecklistCumpleRequisitos),
            userId.Value,
            role,
            name,
            cancellationToken);

        if (!update.Success)
        {
            TempData["Message"] = update.Message ?? "No se pudo guardar antes de firmar.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await _actaService.FirmarAsync(id, userId.Value, role, name, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Acta firmada." : "No se pudo firmar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id });
    }

    private static GuardarActaVerificacionDto ToDto(CreateActaVerificacionForm form) =>
        new(
            form.ProductionOrderId,
            form.FichaId,
            form.Observacion,
            form.CumpleEspecificaciones,
            form.CumpleAcabados,
            form.CumpleSinDefectos,
            form.ChecklistCumpleRequisitos);

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            userId = id;

        var role = User.FindFirstValue(ClaimTypes.Role);
        var name = User.FindFirstValue(ClaimTypes.Name);
        return (userId, role, name);
    }
}
