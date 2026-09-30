using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// El inventario vive en PlantasInventario/Detalle. Estas rutas responden 301 y no modifican stock.
[Authorize]
public class InventarioController : Controller
{
    private readonly ICurrentPlantaInventarioAccessor? _plantaAccessor;

    // Los tres servicios se conservan en la firma para no romper los tests que construyen el controlador.
    public InventarioController(
        IInventoryService inventoryService,
        IProductionOrderService orderService,
        IStockMovementService stockMovements,
        ICurrentPlantaInventarioAccessor? plantaAccessor = null)
    {
        _ = inventoryService;
        _ = orderService;
        _ = stockMovements;
        _plantaAccessor = plantaAccessor;
    }

    // Pantalla principal: el Instructor sigue sin acceso; el resto va a la planta que le corresponde.
    [Authorize(Policy = AuthorizationPolicyNames.PuedeConsultarInventario)]
    [HttpGet]
    public Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (User.IsInRole(UserRoles.Instructor) && !User.IsInRole(UserRoles.Administrador))
            return Task.FromResult<IActionResult>(Forbid());

        return Task.FromResult(RedirectLegacy(movimientos: false));
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    [HttpGet]
    public IActionResult Movimientos(
        DateOnly? desde,
        DateOnly? hasta,
        int? materialId,
        CancellationToken cancellationToken)
    {
        _ = desde;
        _ = hasta;
        _ = materialId;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: true);
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeRegistrarMateriales)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddMaterial([Bind(Prefix = "CreateMaterial")] CreateMaterialForm form, CancellationToken cancellationToken)
    {
        _ = form;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: false);
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditMaterial(EditMaterialForm form, CancellationToken cancellationToken)
    {
        _ = form;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: false);
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AdjustStock(AdjustStockForm form, CancellationToken cancellationToken)
    {
        _ = form;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: false);
    }

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(int MaterialId, MaterialStatus Status, CancellationToken cancellationToken)
    {
        _ = MaterialId;
        _ = Status;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: false);
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateRequest([Bind(Prefix = "CreateRequest")] CreateRequestForm form, CancellationToken cancellationToken)
    {
        _ = form;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: false);
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeAprobarSolicitudes)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveRequest(int id, CancellationToken cancellationToken)
    {
        _ = id;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: false);
    }

    [Authorize(Policy = AuthorizationPolicyNames.PuedeAprobarSolicitudes)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RejectRequest(int id, CancellationToken cancellationToken)
    {
        _ = id;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: false);
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteMaterial(int id, CancellationToken cancellationToken)
    {
        _ = id;
        _ = cancellationToken;
        return RedirectLegacy(movimientos: false);
    }

    private IActionResult RedirectLegacy(bool movimientos)
    {
        if (!movimientos)
            return RedirectPermanent(LegacyInventarioUrl());

        var query = Request?.QueryString.Value ?? string.Empty;
        if (EsEncargadoDeUnaSolaPlanta(out var plantaId)
            && query.Contains("plantaInventarioId", StringComparison.OrdinalIgnoreCase) == false)
        {
            query = string.IsNullOrEmpty(query)
                ? $"?plantaInventarioId={plantaId}"
                : query + $"&plantaInventarioId={plantaId}";
        }

        return RedirectPermanent("/PlantasInventario/Movimientos" + query);
    }

    private string LegacyInventarioUrl()
    {
        if (EsEncargadoDeUnaSolaPlanta(out var plantaId))
            return $"/PlantasInventario/Detalle/{plantaId}";

        if (User.IsInRole(UserRoles.Administrador)
            || User.IsInRole(UserRoles.EncargadoDeBodega))
            return "/PlantasInventario";

        return "/PlantasInventario/Consultar";
    }

    private bool EsEncargadoDeUnaSolaPlanta(out int plantaId)
    {
        plantaId = 0;
        if (!User.IsInRole(UserRoles.EncargadoDeBodega) || User.IsInRole(UserRoles.Administrador))
            return false;

        var ids = _plantaAccessor?.PlantaInventarioIds;
        if (ids is not { Count: 1 })
            return false;

        plantaId = ids[0];
        return plantaId > 0;
    }
}
