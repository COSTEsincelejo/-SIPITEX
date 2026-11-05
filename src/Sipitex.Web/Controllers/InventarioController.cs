using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sipitex.Application.Authorization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// El inventario vive en PlantasInventario/Detalle. Esas rutas responden 301 y no modifican stock.
// Importar es la excepción: el administrador carga la plantilla Excel en dos pasos.
[Authorize]
public class InventarioController : Controller
{
    private readonly ICurrentPlantaInventarioAccessor? _plantaAccessor;
    private readonly IInventarioImportService? _importacion;
    private readonly IMemoryCache? _cache;

    // Los tres servicios se conservan en la firma para no romper los tests que construyen el controlador.
    public InventarioController(
        IInventoryService inventoryService,
        IProductionOrderService orderService,
        IStockMovementService stockMovements,
        ICurrentPlantaInventarioAccessor? plantaAccessor = null,
        IInventarioImportService? importacion = null,
        IMemoryCache? cache = null)
    {
        _ = inventoryService;
        _ = orderService;
        _ = stockMovements;
        _plantaAccessor = plantaAccessor;
        _importacion = importacion;
        _cache = cache;
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpGet]
    public IActionResult Importar()
    {
        return View(new InventarioImportarViewModel
        {
            Message = TempData["ImportMessage"] as string,
            IsSuccess = TempData["ImportOk"] as string == "1"
        });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(InventarioImportTextos.MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = InventarioImportTextos.MaxBytes)]
    public async Task<IActionResult> Importar(IFormFile? archivo, CancellationToken cancellationToken)
    {
        var importacion = ExigirImportacion();
        if (archivo is null || archivo.Length == 0)
            return View(new InventarioImportarViewModel { Message = "Seleccione un archivo .xlsx.", IsSuccess = false });

        var extension = Path.GetExtension(archivo.FileName);
        if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return View(new InventarioImportarViewModel { Message = "Solo se aceptan archivos .xlsx.", IsSuccess = false });

        if (archivo.Length > InventarioImportTextos.MaxBytes)
            return View(new InventarioImportarViewModel { Message = "El archivo supera el máximo de 5 MB.", IsSuccess = false });

        await using var buffer = new MemoryStream();
        await archivo.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        buffer.Position = 0;
        var simulacion = await importacion.SimularAsync(buffer, cancellationToken);
        var token = Guid.NewGuid().ToString("N");
        _cache!.Set(CacheKey(token), bytes, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return View(new InventarioImportarViewModel
        {
            Token = token,
            Resultado = simulacion,
            Message = simulacion.ArchivoValido ? null : simulacion.ErrorArchivo,
            IsSuccess = false
        });
    }

    [Authorize(Roles = UserRoles.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(string? token, CancellationToken cancellationToken)
    {
        var importacion = ExigirImportacion();
        var cache = _cache!;
        if (string.IsNullOrWhiteSpace(token) || cache.Get(CacheKey(token)) is not byte[] bytes)
        {
            TempData["ImportMessage"] = "La simulación expiró. Vuelva a cargar el archivo.";
            TempData["ImportOk"] = "0";
            return RedirectToAction(nameof(Importar));
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) || actorId <= 0)
        {
            TempData["ImportMessage"] = "No se pudo identificar al administrador que confirma.";
            TempData["ImportOk"] = "0";
            return RedirectToAction(nameof(Importar));
        }

        await using var buffer = new MemoryStream(bytes, writable: false);
        var resultado = await importacion.ConfirmarAsync(buffer, actorId, cancellationToken);
        cache.Remove(CacheKey(token));

        TempData["ImportMessage"] = MensajeConfirmacion(resultado);
        var confirmo = resultado.ArchivoValido && (resultado.Importados > 0 || resultado.Errores.Count == 0);
        TempData["ImportOk"] = confirmo ? "1" : "0";
        return RedirectToAction(nameof(Importar));
    }

    private IInventarioImportService ExigirImportacion()
    {
        if (_importacion is null || _cache is null)
            throw new InvalidOperationException("La importación de inventario no está configurada.");
        return _importacion;
    }

    private static string CacheKey(string token) => "inventario-import:" + token;

    private static string MensajeConfirmacion(InventarioImportResult resultado)
    {
        if (!resultado.ArchivoValido)
            return resultado.ErrorArchivo ?? "No se pudo importar el archivo.";

        var importados = resultado.Importados == 1
            ? "Se importó 1 material"
            : $"Se importaron {resultado.Importados} materiales";
        var fichas = resultado.FichasVinculadas == 1
            ? "Se vinculó 1 ficha técnica"
            : $"Se vincularon {resultado.FichasVinculadas} fichas técnicas";
        return $"{importados}. {fichas}. Omitidos: {resultado.Omitidas.Count}. Con error: {resultado.Errores.Count}.";
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

        return "/PlantasInventario";
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
