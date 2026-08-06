using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Materiales, stock, solicitudes y aprobaciones del inventario
[Authorize]
public class InventarioController : Controller
{
    // Servicio que habla con la BD de materiales y solicitudes
    private readonly IInventoryService _inventoryService;
    // Lo necesito para el combo de órdenes al pedir material
    private readonly IProductionOrderService _orderService;

    // ASP.NET inyecta los servicios por constructor
    public InventarioController(IInventoryService inventoryService, IProductionOrderService orderService)
    {
        // Guardo referencia al servicio de inventario
        _inventoryService = inventoryService;
        // Guardo referencia al servicio de órdenes
        _orderService = orderService;
    }

    // Pantalla principal del inventario
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Armo el ViewModel con materiales, solicitudes y combos
        return View(await BuildViewModel(cancellationToken));
    }

    // Agrega un material nuevo al catálogo
    [Authorize(Policy = AuthorizationPolicyNames.PuedeRegistrarMateriales)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaterial([Bind(Prefix = "CreateMaterial")] CreateMaterialForm form, CancellationToken cancellationToken)
    {
        // Llamo al servicio con los datos del formulario
        var result = await _inventoryService.AddMaterialAsync(
            new CreateMaterialDto(form.Name, form.Stock, form.Unit), cancellationToken);

        // Vuelvo a cargar la pantalla completa para mostrar la tabla actualizada
        var vm = await BuildViewModel(cancellationToken);
        // Mensaje que ve el usuario según si salió bien o no
        vm.Message = result.Message ?? (result.Success ? "Material agregado." : "Error al agregar material.");
        // Bandera verde/roja en la vista
        vm.IsSuccess = result.Success;
        // Devuelvo la misma vista con mensaje en vez de redirect (el form queda en la página)
        return View("Index", vm);
    }

    // Ajuste de stock (bodega/admin). Uso TempData porque hago redirect.
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(AdjustStockForm form, CancellationToken cancellationToken)
    {
        // El servicio busca el material y pone el stock nuevo
        var result = await _inventoryService.AdjustStockAsync(
            new AdjustStockDto(form.MaterialId, form.NewStock), cancellationToken);

        // Guardo mensaje para después del redirect
        TempData["Message"] = result.Message ?? (result.Success ? "Stock actualizado." : "Error al ajustar stock.");
        // Igual con el indicador de éxito
        TempData["IsSuccess"] = result.Success;
        // Vuelvo al listado para que se vea el cambio
        return RedirectToAction(nameof(Index));
    }

    // Cambia estado del material (activo, agotado, etc.)
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int MaterialId, MaterialStatus Status, CancellationToken cancellationToken)
    {
        // Actualizo Bueno/Regular/Deteriorado en BD
        var result = await _inventoryService.UpdateStatusAsync(
            new UpdateMaterialStatusDto(MaterialId, Status), cancellationToken);

        // Mensaje flash para la siguiente carga de Index
        TempData["Message"] = result.Message ?? (result.Success ? "Estado actualizado." : "Error al actualizar estado.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Los instructores piden material para una orden; bodega/admin aprueba después
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest([Bind(Prefix = "CreateRequest")] CreateRequestForm form, CancellationToken cancellationToken)
    {
        // Creo solicitud en estado Pendiente
        var result = await _inventoryService.CreateRequestAsync(
            new CreateMaterialRequestDto(form.ProductionOrderId, form.MaterialId, form.Quantity), cancellationToken);

        // Recargo datos de la pantalla
        var vm = await BuildViewModel(cancellationToken);
        vm.Message = result.Message ?? (result.Success ? "Solicitud creada." : "Error al crear solicitud.");
        vm.IsSuccess = result.Success;
        // Me quedo en Index como en AddMaterial
        return View("Index", vm);
    }

    // Aprueba una solicitud y descuenta stock si alcanza
    [Authorize(Policy = AuthorizationPolicyNames.PuedeAprobarSolicitudes)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id, CancellationToken cancellationToken)
    {
        // id es el de la solicitud; el servicio valida stock y descuenta
        var result = await _inventoryService.ApproveRequestAsync(id, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud aprobada." : "No se pudo aprobar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Rechaza la solicitud sin tocar inventario
    [Authorize(Policy = AuthorizationPolicyNames.PuedeAprobarSolicitudes)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id, CancellationToken cancellationToken)
    {
        // Solo cambia el estado a Rechazada
        var result = await _inventoryService.RejectRequestAsync(id, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Solicitud rechazada." : "No se pudo rechazar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Elimina material del catálogo (bloqueado si está en alguna ficha técnica)
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMaterial(int id, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.DeleteMaterialAsync(id, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Material eliminado." : "No se pudo eliminar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    // Arma el ViewModel completo de la pantalla (materiales + solicitudes + combos)
    private async Task<InventarioIndexViewModel> BuildViewModel(CancellationToken cancellationToken)
    {
        // Lista de materiales para la tabla principal
        var materials = await _inventoryService.GetMaterialsAsync(cancellationToken);
        // Órdenes para el dropdown al crear solicitud
        var orders = await _orderService.GetOrdersAsync(cancellationToken);

        // Objeto que la vista Razor consume
        return new InventarioIndexViewModel
        {
            // Tabla de materiales
            Materials = materials,
            // Solicitudes pendientes/aprobadas/rechazadas
            Requests = await _inventoryService.GetRequestsAsync(cancellationToken),
            // Combo de órdenes de producción
            Orders = orders,
            // Form vacío para agregar material
            CreateMaterial = new CreateMaterialForm(),
            // Form de solicitud con valores por defecto en los combos
            // Prefiero dejar seleccionado el primer ítem para que el form no quede vacío
            CreateRequest = new CreateRequestForm
            {
                // Primera orden del listado o 0 si no hay ninguna
                ProductionOrderId = orders.FirstOrDefault()?.Id ?? 0,
                // Primer material o 0
                MaterialId = materials.FirstOrDefault()?.Id ?? 0
            },
            // Mensaje que vino de un POST anterior (TempData)
            Message = TempData["Message"] as string,
            // Si el último POST fue exitoso
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        };
    }
}
