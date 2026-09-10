using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
public class GruposConfeccionController : Controller
{
    private readonly IGrupoConfeccionService _grupos;
    private readonly IProductionOrderService _orders;
    private readonly IBomCatalogService _bom;

    public GruposConfeccionController(
        IGrupoConfeccionService grupos,
        IProductionOrderService orders,
        IBomCatalogService bom)
    {
        _grupos = grupos;
        _orders = orders;
        _bom = bom;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? grupoId, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var vm = new GruposConfeccionIndexViewModel
        {
            Grupos = await _grupos.GetAllAsync(cancellationToken),
            Orders = await _orders.GetOrdersAsync(userId, role, name, cancellationToken),
            Instructors = await _bom.GetAssignableInstructorsAsync(cancellationToken),
            Cruzado = grupoId is int id ? await _grupos.GetConsumosCruzadosAsync(id, cancellationToken) : null,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
        if (userId is int uid && User.IsInRole(UserRoles.Instructor))
            vm.Form.InstructorUserId = uid;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] CreateGrupoConfeccionForm form, CancellationToken cancellationToken)
    {
        var result = await _grupos.CreateAsync(new CreateGrupoConfeccionDto(
            form.ProductionOrderId,
            form.InstructorUserId,
            form.FechaRealizacion,
            form.HoraInicio,
            form.HoraFin,
            form.CantidadPrendas), cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id, TimeOnly horaFin, int cantidadPrendas, CancellationToken cancellationToken)
    {
        var result = await _grupos.CerrarAsync(id, horaFin, cantidadPrendas, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index), new { grupoId = id });
    }

    private (int? UserId, string? Role, string? Name) CurrentViewer()
    {
        int? userId = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            userId = id;
        return (userId, User.FindFirstValue(ClaimTypes.Role), User.FindFirstValue(ClaimTypes.Name));
    }
}
