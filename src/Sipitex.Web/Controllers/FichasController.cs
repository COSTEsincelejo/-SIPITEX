using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class FichasController : Controller
{
    private readonly IFichaService _fichaService;
    private readonly IProductionOrderService _orderService;

    public FichasController(IFichaService fichaService, IProductionOrderService orderService)
    {
        _fichaService = fichaService;
        _orderService = orderService;
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await BuildViewModel(cancellationToken));

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFicha([Bind(Prefix = "CreateFicha")] CreateFichaForm form, CancellationToken cancellationToken)
    {
        var (userId, role, _) = CurrentViewer();
        int? instructorUserId = string.Equals(role, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase)
            ? userId
            : null;

        var result = await _fichaService.CreateFichaAsync(
            new CreateFichaDto(
                form.FichaCode,
                form.ProcessName,
                form.InstructorName,
                form.Turno,
                form.ProductionOrderId is > 0 ? form.ProductionOrderId : null),
            instructorUserId,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Ficha registrada." : "Error al registrar ficha.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterProductionForm form, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var result = await _fichaService.RegisterSessionAsync(
            new RegisterProductionDto(form.ProductionOrderId, form.FichaId, form.Units, form.Observations),
            userId,
            role,
            name,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Sesión registrada." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickRegister(int fichaId, int units, string? observations, CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var result = await _fichaService.QuickRegisterAsync(fichaId, units, observations, userId, role, name, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Registro exitoso." : "Error al registrar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    private async Task<FichasIndexViewModel> BuildViewModel(CancellationToken cancellationToken)
    {
        var (userId, role, name) = CurrentViewer();
        var orders = await _orderService.GetOrdersAsync(cancellationToken);
        var fichas = await _fichaService.GetFichasAsync(userId, role, name, cancellationToken);

        var create = new CreateFichaForm();
        if (User.IsInRole(UserRoles.Instructor) && !string.IsNullOrWhiteSpace(name))
            create.InstructorName = name!;

        return new FichasIndexViewModel
        {
            Fichas = fichas,
            Orders = orders,
            Sessions = await _fichaService.GetRecentSessionsAsync(userId, role, name, cancellationToken),
            IsAdministrator = User.IsInRole(UserRoles.Administrador),
            CreateFicha = create,
            Register = new RegisterProductionForm
            {
                ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0,
                FichaId = fichas.FirstOrDefault()?.Id ?? 0
            },
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }

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
