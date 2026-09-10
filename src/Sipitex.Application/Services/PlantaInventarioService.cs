using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// CRUD de plantasInventario. Nombre único (case-insensitive). El borrado es físico
// y se rechaza si hay dependencias o si es la última / la plantaInventario por defecto.
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

        if (await _plantas.CountAsync(cancellationToken) <= 1)
            return ServiceResult.Fail("No se puede eliminar la última planta de inventario del sistema.");

        var deps = await _plantas.CountDependenciasAsync(id, cancellationToken);
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
                $"No se puede eliminar «{plantaInventario.Nombre}»: tiene {string.Join(", ", partes)} asociados. Reasígnelos o elimínelos antes.");
        }

        _plantas.Remove(plantaInventario);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Planta de inventario «{plantaInventario.Nombre}» eliminada.");
    }

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
