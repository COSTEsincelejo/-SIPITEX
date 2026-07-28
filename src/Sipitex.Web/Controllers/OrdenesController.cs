using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

[Authorize]
public class OrdenesController : Controller
{
    private readonly IProductionOrderService _orderService;
    private readonly IUserAccountService _userService;

    public OrdenesController(IProductionOrderService orderService, IUserAccountService userService)
    {
        _orderService = orderService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await BuildViewModel(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateOrder")] CreateOrderForm form, CancellationToken cancellationToken)
    {
        var instructorId = ResolveInstructorId(form.InstructorId);
        var result = await _orderService.CreateOrderAsync(
            new CreateProductionOrderDto(form.ProductName, form.TotalQuantity, form.Deadline, instructorId),
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Orden creada." : "Error al crear orden.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduction(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.RegisterProductionAsync(id, 10, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Producción registrada." : "Error en producción.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    private int? ResolveInstructorId(int? formInstructorId)
    {
        // Si entra un instructor, la orden queda ligada a su cuenta (aunque no elija en el combo).
        if (User.IsInRole(UserRoles.Instructor) &&
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId))
        {
            return currentId;
        }

        return formInstructorId is > 0 ? formInstructorId : null;
    }

    private async Task<OrdenesIndexViewModel> BuildViewModel(CancellationToken cancellationToken)
    {
        var instructors = (await _userService.GetUsersAsync(cancellationToken))
            .Where(u => u.IsActive && u.Rol == UserRoles.Instructor)
            .OrderBy(u => u.Nombre)
            .Select(u => new InstructorOption(u.Id, u.Nombre, u.Email))
            .ToList();

        var create = new CreateOrderForm();
        if (User.IsInRole(UserRoles.Instructor) &&
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId))
        {
            create.InstructorId = currentId;
        }

        return new OrdenesIndexViewModel
        {
            Orders = await _orderService.GetOrdersAsync(cancellationToken),
            KnownProducts = await _orderService.GetKnownProductNamesAsync(cancellationToken),
            Instructors = instructors,
            CreateOrder = create,
            CurrentUserIsInstructor = User.IsInRole(UserRoles.Instructor)
        };
    }
}
