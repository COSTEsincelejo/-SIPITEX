using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Órdenes de producción: listar, crear, materiales, flujo MES y registrar avance
[Authorize]
public class OrdenesController : Controller
{
    private readonly IProductionOrderService _orderService;
    private readonly IBomCatalogService _bomCatalog;
    private readonly IOrderMaterialService _orderMaterialService;
    private readonly IInventoryService _inventoryService;
    private readonly IProductionFlowService _flowService;
    private readonly IUserAccountService _users;

    public OrdenesController(
        IProductionOrderService orderService,
        IBomCatalogService bomCatalog,
        IOrderMaterialService orderMaterialService,
        IInventoryService inventoryService,
        IProductionFlowService flowService,
        IUserAccountService users)
    {
        _orderService = orderService;
        _bomCatalog = bomCatalog;
        _orderMaterialService = orderMaterialService;
        _inventoryService = inventoryService;
        _flowService = flowService;
        _users = users;
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new OrdenesIndexViewModel
        {
            Orders = await _orderService.GetOrdersAsync(cancellationToken: cancellationToken),
            ProductNames = await _bomCatalog.GetOrderEligibleProductNamesAsync(cancellationToken),
            CreateOrder = new CreateOrderForm(),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    // Detalle MES completo (materiales + etapas + historial + inventario terminado)
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var (userId, role, name) = GetActor();
        var mes = await _flowService.GetMesDetailAsync(id, userId, role, cancellationToken);
        if (mes is null) return NotFound();

        var instructors = await _users.GetUsersAsync(cancellationToken);
        return View(new OrdenMesDetailViewModel
        {
            Mes = mes,
            Materials = await _inventoryService.GetMaterialsAsync(cancellationToken),
            Instructors = instructors.Where(u => u.Rol == UserRoles.Instructor && u.IsActive).ToList(),
            AddMaterial = new AddOrderMaterialForm { OrderId = id, QuantityRequired = 1 },
            ChangeLogs = await _orderService.GetChangeLogAsync(id, cancellationToken),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersAsync(cancellationToken: cancellationToken);
        var order = orders.FirstOrDefault(o => o.Id == id);
        if (order is null) return NotFound();

        return View(new OrdenEditViewModel
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            ProductNames = await _bomCatalog.GetOrderEligibleProductNamesAsync(cancellationToken),
            Form = new EditOrderForm
            {
                OrderId = order.Id,
                ProductName = order.ProductName,
                TotalQuantity = order.TotalQuantity,
                Deadline = order.Deadline,
                ClientName = order.ClientName
            },
            ChangeLogs = await _orderService.GetChangeLogAsync(id, cancellationToken),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "Form")] EditOrderForm form, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actorId))
        {
            TempData["Message"] = "Sesión no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Edit), new { id = form.OrderId });
        }

        var result = await _orderService.UpdateOrderAsync(
            new UpdateProductionOrderDto(form.OrderId, form.ProductName, form.TotalQuantity, form.Deadline, form.ClientName),
            actorId,
            cancellationToken);

        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(result.Success ? nameof(Detail) : nameof(Edit), new { id = form.OrderId });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var actorId))
        {
            TempData["Message"] = "Sesión no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await _orderService.CancelOrderAsync(id, actorId, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeCrearOrdenes)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateOrder")] CreateOrderForm form, CancellationToken cancellationToken)
    {
        int? responsibleInstructorId = null;
        if (User.IsInRole(UserRoles.Instructor)
            && !User.IsInRole(UserRoles.Administrador)
            && TryGetUserId(out var instructorId))
        {
            responsibleInstructorId = instructorId;
        }

        var result = await _orderService.CreateOrderAsync(
            new CreateProductionOrderDto(
                form.ProductName,
                form.TotalQuantity,
                form.Deadline,
                form.ClientName,
                responsibleInstructorId),
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Orden creada." : "Error al crear orden.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduction(int id, int units, CancellationToken cancellationToken)
    {
        var result = await _orderService.RegisterProductionAsync(id, units, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Producción registrada." : "Error en producción.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaterial([Bind(Prefix = "AddMaterial")] AddOrderMaterialForm form, CancellationToken cancellationToken)
    {
        var result = await _orderMaterialService.AddMaterialAsync(
            new AddOrderMaterialDto(form.OrderId, form.MaterialId, form.QuantityRequired, form.Observations),
            cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id = form.OrderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMaterial(int lineId, int orderId, CancellationToken cancellationToken)
    {
        var result = await _orderMaterialService.RemoveMaterialAsync(lineId, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportBomMaterials(int id, CancellationToken cancellationToken)
    {
        var result = await _orderMaterialService.ImportFromBomAsync(id, cancellationToken);
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detail), new { id });
    }

    // --- Flujo MES ---

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStage(int orderId, string name, bool isOptional, CancellationToken cancellationToken)
    {
        var (uid, _, uname) = GetActor();
        Flash(await _flowService.AddStageAsync(new AddOrderStageDto(orderId, name, isOptional), uid, uname, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStage(int stageId, int orderId, CancellationToken cancellationToken)
    {
        var (uid, _, uname) = GetActor();
        Flash(await _flowService.RemoveStageAsync(stageId, uid, uname, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveStage(int stageId, int orderId, int direction, CancellationToken cancellationToken)
    {
        var (uid, _, uname) = GetActor();
        Flash(await _flowService.MoveStageAsync(stageId, direction, uid, uname, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartStage(int stageId, int orderId, CancellationToken cancellationToken)
    {
        var (uid, role, uname) = GetActor();
        Flash(await _flowService.StartStageAsync(stageId, uid, uname, role, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PauseStage(int stageId, int orderId, CancellationToken cancellationToken)
    {
        var (uid, role, uname) = GetActor();
        Flash(await _flowService.PauseStageAsync(stageId, uid, uname, role, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResumeStage(int stageId, int orderId, CancellationToken cancellationToken)
    {
        var (uid, role, uname) = GetActor();
        Flash(await _flowService.ResumeStageAsync(stageId, uid, uname, role, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteStage(int stageId, int orderId, CancellationToken cancellationToken)
    {
        var (uid, role, uname) = GetActor();
        Flash(await _flowService.CompleteStageAsync(stageId, uid, uname, role, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignInstructor(int stageId, int orderId, int? instructorUserId, CancellationToken cancellationToken)
    {
        var (uid, _, uname) = GetActor();
        Flash(await _flowService.AssignInstructorAsync(new AssignStageInstructorDto(stageId, instructorUserId), uid, uname, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessUnits(int stageId, int orderId, int quantity, string? observations, CancellationToken cancellationToken)
    {
        var (uid, role, uname) = GetActor();
        Flash(await _flowService.ProcessUnitsAsync(new ProcessStageUnitsDto(stageId, quantity, observations), uid, uname, role, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToNext(int stageId, int orderId, int quantity, string? observations, CancellationToken cancellationToken)
    {
        var (uid, role, uname) = GetActor();
        Flash(await _flowService.SendToNextAsync(new SendToNextStageDto(stageId, quantity, observations), uid, uname, role, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PartialInventoryIn(int orderId, int stageId, int quantity, string? observations, CancellationToken cancellationToken)
    {
        var (uid, role, uname) = GetActor();
        Flash(await _flowService.PartialInventoryInAsync(new PartialInventoryInDto(orderId, stageId, quantity, observations), uid, uname, role, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PartialWithdraw(int stageId, int orderId, int quantity, string motive, string? observations, CancellationToken cancellationToken)
    {
        var (uid, role, uname) = GetActor();
        Flash(await _flowService.PartialWithdrawAsync(
            new PartialWithdrawalDto(stageId, quantity, motive, observations, uid), uid, uname, role, cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStagePermission(int orderId, int userId, string stageName, bool allowed, CancellationToken cancellationToken)
    {
        Flash(await _flowService.SetStagePermissionAsync(new UpsertStagePermissionDto(userId, stageName, allowed), cancellationToken));
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    private void Flash(ServiceResult result)
    {
        TempData["Message"] = result.Message;
        TempData["IsSuccess"] = result.Success;
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    private (int UserId, string Role, string Name) GetActor()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "Usuario";
        return (id, role, name);
    }
}
