using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Import;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// El inventario operativo está en PlantasInventario. Aquí queda la importación Excel
// y la redirección de las URLs viejas /Inventario, /Inventario/Index y /Inventario/Movimientos.
[Authorize]
public class InventarioController : Controller
{
    private const string ContentTypeXlsx =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IInventarioImportService? _importacion;
    private readonly IMemoryCache? _cache;
    private readonly IWebHostEnvironment? _env;

    public InventarioController(
        IInventarioImportService? importacion = null,
        IMemoryCache? cache = null,
        IWebHostEnvironment? env = null)
    {
        _importacion = importacion;
        _cache = cache;
        _env = env;
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
    [HttpGet]
    public IActionResult DescargarPlantilla()
    {
        foreach (var path in RutasPlantilla())
        {
            if (System.IO.File.Exists(path))
                return PhysicalFile(path, ContentTypeXlsx, InventarioPlantillaExcel.FileName);
        }

        return File(InventarioPlantillaExcel.Crear(), ContentTypeXlsx, InventarioPlantillaExcel.FileName);
    }

    private IEnumerable<string> RutasPlantilla()
    {
        if (_env is not null)
        {
            yield return Path.Combine(_env.WebRootPath, "plantillas", InventarioPlantillaExcel.FileName);
            yield return Path.Combine(_env.ContentRootPath, "plantillas", InventarioPlantillaExcel.FileName);
            var repoDocs = Path.GetFullPath(Path.Combine(
                _env.ContentRootPath, "..", "..", "docs", "plantillas", InventarioPlantillaExcel.FileName));
            yield return repoDocs;
        }
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

    [HttpGet]
    public IActionResult Index() => RedirectPermanent("/PlantasInventario/Index");

    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    [HttpGet]
    public IActionResult Movimientos()
    {
        var query = Request?.QueryString.Value ?? string.Empty;
        return RedirectPermanent("/PlantasInventario/Movimientos" + query);
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
}
