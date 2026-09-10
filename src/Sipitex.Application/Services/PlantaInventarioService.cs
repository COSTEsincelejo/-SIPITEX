using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// CRUD de plantas de inventario. Nombre único (case-insensitive).
// La baja es lógica (Activo=false) para no romper historial. Con stock se exige reasignación.
public class PlantaInventarioService : IPlantaInventarioService
{
    private const int MaxNombreLength = 80;
    // Alineado a SolicitudMaterialService.DefaultPlantaInventarioId (InsumosLibres / backfill AddPlantasInventario).
    private const int DefaultPlantaInventarioId = 1;

    private readonly IPlantaInventarioRepository _plantas;
    private readonly IUnitOfWork _unitOfWork;

    public PlantaInventarioService(IPlantaInventarioRepository plantasInventario, IUnitOfWork unitOfWork)
    {
        _plantas = plantasInventario;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<PlantaInventario>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _plantas.GetAllAsync(cancellationToken);

    public Task<PlantaInventario?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _plantas.GetByIdAsync(id, cancellationToken);

    public async Task<ServiceResult> CreateAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var validated = await ValidateNombreAsync(nombre, excludeId: null, cancellationToken);
        if (validated.Error is not null)
            return ServiceResult.Fail(validated.Error);

        await _plantas.AddAsync(new PlantaInventario { Nombre = validated.Nombre }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Planta de inventario «{validated.Nombre}» creada.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, string nombre, CancellationToken cancellationToken = default)
    {
        var plantaInventario = await _plantas.GetByIdAsync(id, cancellationToken);
        if (plantaInventario is null)
            return ServiceResult.Fail("Planta de inventario no encontrada.");

        var validated = await ValidateNombreAsync(nombre, excludeId: id, cancellationToken);
        if (validated.Error is not null)
            return ServiceResult.Fail(validated.Error);

        plantaInventario.Nombre = validated.Nombre;
        _plantas.Update(plantaInventario);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Planta de inventario «{validated.Nombre}» actualizada.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var plantaInventario = await _plantas.GetByIdAsync(id, cancellationToken);
        if (plantaInventario is null)
            return ServiceResult.Fail("Planta de inventario no encontrada.");

        if (id == DefaultPlantaInventarioId)
            return ServiceResult.Fail(
                "No se puede eliminar Planta de Inventario 1: el sistema la usa como planta de inventario por defecto al crear solicitudes de insumos libres.");

        if (await _plantas.CountActivasAsync(cancellationToken) <= 1)
            return ServiceResult.Fail("No se puede eliminar la última planta de inventario activa del sistema.");

        if (!plantaInventario.Activo)
            return ServiceResult.Fail("La planta de inventario ya está inactiva.");

        var deps = await _plantas.CountDependenciasAsync(id, cancellationToken);
        if (deps.HasStock)
            return ServiceResult.Fail(
                $"No se puede eliminar «{plantaInventario.Nombre}»: tiene stock asociado ({deps.StockTotal} unidades). " +
                "Reasigne el inventario a otra planta de inventario antes de eliminar.");

        if (deps.Any)
        {
            var partes = new List<string>();
            if (deps.Materiales > 0)
                partes.Add($"{deps.Materiales} material(es)");
            if (deps.Solicitudes > 0)
                partes.Add($"{deps.Solicitudes} solicitud(es)");
            if (deps.Encargados > 0)
                partes.Add($"{deps.Encargados} encargado(s) de bodega");
            return ServiceResult.Fail(
                $"No se puede eliminar «{plantaInventario.Nombre}»: tiene {string.Join(", ", partes)} asociados. " +
                "Use la reasignación a otra planta o elimínelos antes.");
        }

        plantaInventario.Activo = false;
        _plantas.Update(plantaInventario);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Planta de inventario «{plantaInventario.Nombre}» desactivada.");
    }

    public Task<PlantaInventarioDependencias> GetDependenciasAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _plantas.CountDependenciasAsync(id, cancellationToken);

    private async Task<(string Nombre, string? Error)> ValidateNombreAsync(
        string nombre,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return ("", "El nombre de la planta de inventario es obligatorio.");

        var trimmed = nombre.Trim();
        if (trimmed.Length > MaxNombreLength)
            return ("", $"El nombre no puede superar {MaxNombreLength} caracteres.");

        if (await _plantas.ExistsByNombreAsync(trimmed, cancellationToken, excludeId))
            return ("", "Ya existe una planta de inventario con ese nombre.");

        return (trimmed, null);
    }
}
