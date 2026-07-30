using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Órdenes de producción: listar, crear y registrar avance
[Authorize]
public class OrdenesController : Controller
{
    private readonly IProductionOrderService _orderService;

    // Todo pasa por el servicio de órdenes de producción
    public OrdenesController(IProductionOrderService orderService) => _orderService = orderService;

    // Tabla de órdenes y formulario para crear una nueva
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Bodeguero},{UserRoles.Instructor}")]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Armo el modelo con la lista y el form vacío
        return View(new OrdenesIndexViewModel
        {
            // Traigo todas las órdenes con % de avance
            Orders = await _orderService.GetOrdersAsync(cancellationToken),
            // Formulario para crear orden nueva
            CreateOrder = new CreateOrderForm(),
            // Mensaje del último POST si hubo
            Message = TempData["Message"] as string,
            // Verde o rojo según éxito
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    // Solo admin puede crear órdenes nuevas
    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateOrder")] CreateOrderForm form, CancellationToken cancellationToken)
    {
        // Creo la orden con producto, cantidad total y fecha límite
        var result = await _orderService.CreateOrderAsync(
            new CreateProductionOrderDto(form.ProductName, form.TotalQuantity, form.Deadline), cancellationToken);

        // Guardo feedback para la siguiente carga de Index
        TempData["Message"] = result.Message ?? (result.Success ? "Orden creada." : "Error al crear orden.");
        TempData["IsSuccess"] = result.Success;
        // Redirect para evitar re-post al refrescar
        return RedirectToAction(nameof(Index));
    }

    // Botón rápido: suma 10 unidades a la orden (para pruebas/demo en el taller)
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.Instructor}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduction(int id, CancellationToken cancellationToken)
    {
        // id = orden; siempre sumo 10 unidades en este atajo
        var result = await _orderService.RegisterProductionAsync(id, 10, cancellationToken);
        TempData["Message"] = result.Message ?? (result.Success ? "Producción registrada." : "Error en producción.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }
}
