using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sipitex.Application.Authorization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Models;

namespace Sipitex.Web.Controllers;

// Catálogo de plantasInventario (CRUD: solo Administrador) y consulta de inventario por planta.
[Authorize]
public class PlantasInventarioController : Controller
{
    private readonly IPlantaInventarioService _plantas;
    private readonly IPlantaInventarioReassignmentService _reassignment;
    private readonly IActivityLogService _activityLog;
    private readonly IInventoryService _inventory;
    private readonly ICurrentPlantaInventarioAccessor _plantaAccessor;
    private readonly IStockMovementService? _stockMovements;

    public PlantasInventarioController(
        IPlantaInventarioService plantasInventario,
        IPlantaInventarioReassignmentService reassignment,
        IActivityLogService activityLog,
        IInventoryService inventory,
        ICurrentPlantaInventarioAccessor plantaAccessor,
        IStockMovementService? stockMovements = null)
    {
        _plantas = plantasInventario;
        _reassignment = reassignment;
        _activityLog = activityLog;
        _inventory = inventory;
        _plantaAccessor = plantaAccessor;
        _stockMovements = stockMovements;
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega},{UserRoles.Instructor}")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var plantas = await _plantas.GetAllAsync(cancellationToken);
        if (User.IsInRole(UserRoles.EncargadoDeBodega) && !User.IsInRole(UserRoles.Administrador))
        {
            var allowed = _plantaAccessor.PlantaInventarioIds;
            if (allowed is not { Count: > 0 })
            {
                return View(new PlantasInventarioIndexViewModel
                {
                    SinPlantasAsignadas = true,
                    Message = TempData["Message"] as string,
                    IsSuccess = TempData["IsSuccess"] as bool? ?? false
                });
            }

            var visibles = VisiblePlantas(plantas);
            if (visibles.Count == 0)
            {
                return View(new PlantasInventarioIndexViewModel
                {
                    SinPlantasAsignadas = true,
                    Message = TempData["Message"] as string,
                    IsSuccess = TempData["IsSuccess"] as bool? ?? false
                });
            }

            if (visibles.Count == 1)
                return RedirectToAction(nameof(Detalle), new { id = visibles[0].Id });

            return View(new PlantasInventarioIndexViewModel
            {
                PlantasInventario = visibles,
                Message = TempData["Message"] as string,
                IsSuccess = TempData["IsSuccess"] as bool? ?? false
            });
        }

        return View(new PlantasInventarioIndexViewModel
        {
            PlantasInventario = plantas,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega},{UserRoles.Instructor}")]
    public async Task<IActionResult> Detalle(
        int id,
        string? busqueda,
        string? categoria,
        CancellationToken cancellationToken = default,
        string? nivel = null)
    {
        var acceso = await AutorizarPlantaAsync(id, cancellationToken);
        if (acceso.Error is not null)
            return acceso.Error;

        var planta = acceso.Planta!;
        var stock = await _inventory.GetStockByPlantaDetalleAsync(id, cancellationToken);
        var nivelesPlanta = stock
            .Select(m => StockNivelHelper.Classify(m.Stock, m.MinStock))
            .ToList();
        var filtrados = PlantaDetalleConsulta.Apply(stock, busqueda, categoria);
        if (!string.IsNullOrWhiteSpace(nivel))
            filtrados = filtrados.Where(m => CoincideNivel(m, nivel)).ToList();

        var esAdmin = User.IsInRole(UserRoles.Administrador);
        var esEncargado = User.IsInRole(UserRoles.EncargadoDeBodega);
        var varias = _plantaAccessor.PlantaInventarioIds is { Count: > 1 };
        var soloLectura = !esAdmin && !esEncargado;

        return View(new PlantaDetalleViewModel
        {
            Id = planta.Id,
            Nombre = planta.Nombre,
            Activa = planta.Activo,
            Busqueda = busqueda,
            Categoria = categoria,
            Nivel = nivel,
            MostrarVolverAlCatalogo = esAdmin || soloLectura || varias,
            Materiales = filtrados.Select(m => new MaterialPlantaItem
            {
                Id = m.Id,
                Codigo = m.Code,
                Nombre = m.Name,
                Categoria = PlantaDetalleConsulta.Etiqueta(m.EnFichaTecnica),
                Unidad = m.Unit,
                UnidadMedida = UnitHelper.ToDisplay(m.Unit),
                StockActual = m.Stock,
                MinStock = m.MinStock,
                Estado = m.Status,
                UltimaEntrada = m.LastEntryDate,
                CostoAdquisicion = m.CostoAdquisicion,
                CostoPromedioPonderado = m.CostoPromedioPonderado,
                NivelStock = StockNivelHelper.Classify(m.Stock, m.MinStock)
            }).ToList(),
            TotalItems = stock.Count,
            TotalBajo = nivelesPlanta.Count(n => n == StockNivel.Bajo),
            TotalCritico = nivelesPlanta.Count(n => n == StockNivel.Critico),
            TotalSinFiltro = stock.Count,
            NombresAlerta = stock
                .Where(m => StockNivelHelper.Classify(m.Stock, m.MinStock) != StockNivel.Ok)
                .Select(m => m.Name)
                .ToList(),
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega},{UserRoles.Instructor}")]
    public async Task<IActionResult> Movimientos(
        DateOnly? desde,
        DateOnly? hasta,
        int? materialId,
        int? plantaInventarioId,
        CancellationToken cancellationToken)
    {
        if (_stockMovements is null)
            return StatusCode(StatusCodes.Status500InternalServerError);

        if (plantaInventarioId is int plantaId)
        {
            var acceso = await AutorizarPlantaAsync(plantaId, cancellationToken);
            if (acceso.Error is not null)
                return acceso.Error;
        }
        else if (!User.IsInRole(UserRoles.Administrador) && !User.IsInRole(UserRoles.Instructor))
        {
            var allowed = _plantaAccessor.PlantaInventarioIds;
            if (allowed is null)
                return Forbid();
        }

        var materials = await _inventory.GetMaterialsByPlantaAsync(plantaInventarioId, cancellationToken);
        var movements = await _stockMovements.GetHistoryAsync(desde, hasta, materialId, cancellationToken);
        if (plantaInventarioId is int)
        {
            var ids = materials.Select(m => m.Id).ToHashSet();
            movements = movements.Where(m => ids.Contains(m.MaterialId)).ToList();
        }

        string? plantaNombre = null;
        if (plantaInventarioId is int pid)
            plantaNombre = (await _plantas.GetByIdAsync(pid, cancellationToken))?.Nombre;

        return View(new InventarioMovimientosViewModel
        {
            Movimientos = movements,
            Materials = materials,
            Desde = desde,
            Hasta = hasta,
            MaterialId = materialId,
            PlantaInventarioId = plantaInventarioId,
            PlantaNombre = plantaNombre
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicyNames.PuedeRegistrarMateriales)]
    public async Task<IActionResult> AddMaterial(
        int id,
        [Bind(Prefix = "CreateMaterial")] CreateMaterialForm form,
        CancellationToken cancellationToken)
    {
        if (RechazarSiNoPuedeEscribir() is { } lectura)
            return lectura;

        var acceso = await AutorizarPlantaAsync(id, cancellationToken);
        if (acceso.Error is not null)
            return acceso.Error;

        if (!TryGetActorUserId(out var actorId))
        {
            TempData["Message"] = "Sesión no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var result = await _inventory.AddMaterialAsync(
            new CreateMaterialDto(form.Name, form.Stock, form.Unit, form.Origen, form.CostoAdquisicion, id),
            actorId,
            cancellationToken);

        TempData["Message"] = result.Message ?? (result.Success ? "Material agregado." : "Error al agregar material.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> EditMaterial(int id, EditMaterialForm form, CancellationToken cancellationToken)
    {
        if (RechazarSiNoEsAdministrador() is { } soloAdmin)
            return soloAdmin;

        var acceso = await AutorizarPlantaAsync(id, cancellationToken);
        if (acceso.Error is not null)
            return acceso.Error;

        if (!await _inventory.MaterialPerteneceAPlantaAsync(form.MaterialId, id, cancellationToken))
            return NotFound();

        var result = await _inventory.UpdateMaterialAsync(
            new UpdateMaterialDto(form.MaterialId, form.Name, form.Unit, form.MinStock, form.CostoAdquisicion),
            cancellationToken,
            id);

        TempData["Message"] = result.Message ?? (result.Success ? "Material actualizado." : "Error al actualizar material.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    public async Task<IActionResult> AdjustStock(int id, AdjustStockForm form, CancellationToken cancellationToken)
    {
        if (RechazarSiNoPuedeEscribir() is { } lectura)
            return lectura;

        var acceso = await AutorizarPlantaAsync(id, cancellationToken);
        if (acceso.Error is not null)
            return acceso.Error;

        if (!TryGetActorUserId(out var actorId))
        {
            TempData["Message"] = "Sesión no válida.";
            TempData["IsSuccess"] = false;
            return RedirectToAction(nameof(Detalle), new { id });
        }

        if (!await _inventory.MaterialPerteneceAPlantaAsync(form.MaterialId, id, cancellationToken))
            return NotFound();

        var result = await _inventory.AdjustStockAsync(
            new AdjustStockDto(form.MaterialId, form.NewStock, form.Origen, form.PrecioUnitario),
            actorId,
            cancellationToken,
            id);

        TempData["Message"] = result.Message ?? (result.Success ? "Stock actualizado." : "Error al ajustar stock.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{UserRoles.Administrador},{UserRoles.EncargadoDeBodega}")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        int materialId,
        MaterialStatus status,
        CancellationToken cancellationToken)
    {
        if (RechazarSiNoPuedeEscribir() is { } lectura)
            return lectura;

        var acceso = await AutorizarPlantaAsync(id, cancellationToken);
        if (acceso.Error is not null)
            return acceso.Error;

        if (!await _inventory.MaterialPerteneceAPlantaAsync(materialId, id, cancellationToken))
            return NotFound();

        var result = await _inventory.UpdateStatusAsync(
            new UpdateMaterialStatusDto(materialId, status),
            cancellationToken,
            id);

        TempData["Message"] = result.Message ?? (result.Success ? "Estado actualizado." : "Error al actualizar estado.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> DeleteMaterial(int id, int materialId, CancellationToken cancellationToken)
    {
        if (RechazarSiNoEsAdministrador() is { } soloAdmin)
            return soloAdmin;

        var acceso = await AutorizarPlantaAsync(id, cancellationToken);
        if (acceso.Error is not null)
            return acceso.Error;

        if (!await _inventory.MaterialPerteneceAPlantaAsync(materialId, id, cancellationToken))
            return NotFound();

        var result = await _inventory.DeleteMaterialAsync(materialId, cancellationToken, id);
        TempData["Message"] = result.Message ?? (result.Success ? "Material eliminado." : "No se pudo eliminar.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "Form")] CreatePlantaInventarioForm form,
        CancellationToken cancellationToken)
    {
        var result = await _plantas.CreateAsync(form.Nombre, cancellationToken);
        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                "CreatePlantaInventario",
                "PlantaInventario",
                entityId: form.Nombre?.Trim(),
                details: $"Nombre={form.Nombre?.Trim()}",
                cancellationToken);
        }

        TempData["Message"] = result.Message ?? (result.Success ? "Planta de inventario creada." : "No se pudo crear la planta de inventario.");
        TempData["IsSuccess"] = result.Success;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var plantaInventario = await _plantas.GetByIdAsync(id, cancellationToken);
        if (plantaInventario is null)
            return NotFound();

        return View(new EditPlantaInventarioViewModel
        {
            Id = plantaInventario.Id,
            Nombre = plantaInventario.Nombre,
            Message = TempData["Message"] as string,
            IsSuccess = TempData["IsSuccess"] as bool? ?? false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Edit(int id, EditPlantaInventarioViewModel model, CancellationToken cancellationToken)
    {
        var result = await _plantas.UpdateAsync(id, model.Nombre, cancellationToken);
        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                "UpdatePlantaInventario",
                "PlantaInventario",
                entityId: id.ToString(),
                details: $"Nombre={model.Nombre?.Trim()}",
                cancellationToken);
            TempData["Message"] = result.Message ?? "Planta de inventario actualizada.";
            TempData["IsSuccess"] = true;
            return RedirectToAction(nameof(Index));
        }

        model.Id = id;
        model.Message = result.Message ?? "No se pudo actualizar la plantaInventario.";
        model.IsSuccess = false;
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var planta = await _plantas.GetByIdAsync(id, cancellationToken);
        if (planta is null)
            return NotFound();

        var deps = await _plantas.GetDependenciasAsync(id, cancellationToken);
        var destinos = (await _plantas.GetAllAsync(cancellationToken))
            .Where(p => p.Activo && p.Id != id)
            .ToList();

        return View(new DeletePlantaInventarioViewModel
        {
            Id = planta.Id,
            Nombre = planta.Nombre,
            Activo = planta.Activo,
            Materiales = deps.Materiales,
            Solicitudes = deps.Solicitudes,
            Encargados = deps.Encargados,
            StockTotal = deps.StockTotal,
            Destinos = destinos,
            DestinoId = destinos.FirstOrDefault()?.Id ?? 0,
            Message = TempData["Message"] as string
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.Administrador)]
    public async Task<IActionResult> Delete(int id, int? destinoId, CancellationToken cancellationToken)
    {
        var deps = await _plantas.GetDependenciasAsync(id, cancellationToken);
        ServiceResult result;
        string action = ActivityLogActions.DeletePlantaInventario;

        if (deps.Any)
        {
            if (destinoId is null or <= 0)
            {
                TempData["Message"] = "Seleccione una planta de inventario destino para reasignar el stock y las dependencias.";
                TempData["IsSuccess"] = false;
                return RedirectToAction(nameof(Delete), new { id });
            }

            result = await _reassignment.ReassignAndDeleteAsync(id, destinoId.Value, cancellationToken);
            action = ActivityLogActions.ReassignPlantaInventario;
        }
        else
        {
            result = await _plantas.DeleteAsync(id, cancellationToken);
        }

        if (result.Success && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId) && actorId > 0)
        {
            await _activityLog.LogAsync(
                actorId,
                action,
                "PlantaInventario",
                entityId: id.ToString(),
                details: result.Message,
                cancellationToken);
        }

        TempData["Message"] = result.Message ?? (result.Success ? "Planta de inventario actualizada." : "No se pudo eliminar la planta de inventario.");
        TempData["IsSuccess"] = result.Success;
        return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Delete), new { id });
    }

    private async Task<(IActionResult? Error, PlantaInventario? Planta)> AutorizarPlantaAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var esAdmin = User.IsInRole(UserRoles.Administrador);
        var esEncargado = User.IsInRole(UserRoles.EncargadoDeBodega);
        var esInstructor = User.IsInRole(UserRoles.Instructor);
        if (!esAdmin && !esEncargado && !esInstructor)
            return (Forbid(), null);

        var planta = await _plantas.GetByIdAsync(id, cancellationToken);
        if (planta is null)
            return (NotFound(), null);

        // El encargado solo opera plantas de su accessor. Admin e Instructor (solo lectura) no usan esa lista.
        if (esEncargado && !esAdmin)
        {
            var allowed = _plantaAccessor.PlantaInventarioIds;
            if (allowed is null || !allowed.Contains(id))
                return (Forbid(), null);
        }

        return (null, planta);
    }

    private static bool CoincideNivel(MaterialPlantaStockDto item, string nivel)
    {
        var actual = StockNivelHelper.Classify(item.Stock, item.MinStock);
        if (string.Equals(nivel, InventarioConsultaFilter.Faltantes, StringComparison.OrdinalIgnoreCase))
            return actual is StockNivel.Bajo or StockNivel.Critico;

        return Enum.TryParse<StockNivel>(nivel, ignoreCase: true, out var parsed) && actual == parsed;
    }

    private IActionResult? RechazarSiNoPuedeEscribir() =>
        User.IsInRole(UserRoles.Administrador) || User.IsInRole(UserRoles.EncargadoDeBodega)
            ? null
            : Forbid();

    private IActionResult? RechazarSiNoEsAdministrador() =>
        User.IsInRole(UserRoles.Administrador) ? null : Forbid();

    private bool TryGetActorUserId(out int userId)
    {
        userId = 0;
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;
    }

    private IReadOnlyList<PlantaInventario> VisiblePlantas(IReadOnlyList<PlantaInventario> plantas)
    {
        var allowed = _plantaAccessor.PlantaInventarioIds;
        if (allowed is null)
            return plantas;

        return plantas.Where(p => allowed.Contains(p.Id)).ToList();
    }
}
