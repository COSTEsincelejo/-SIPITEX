using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// CRUD de bodegas. Nombre único (case-insensitive). El borrado es físico
// y se rechaza si hay dependencias o si es la última / la bodega por defecto.
public class BodegaService : IBodegaService
{
    private const int MaxNombreLength = 80;
    // Alineado a SolicitudMaterialService.DefaultBodegaId (InsumosLibres / backfill AddBodegas).
    private const int DefaultBodegaId = 1;

    private readonly IBodegaRepository _bodegas;
    private readonly IUnitOfWork _unitOfWork;

    public BodegaService(IBodegaRepository bodegas, IUnitOfWork unitOfWork)
    {
        _bodegas = bodegas;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<Bodega>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _bodegas.GetAllAsync(cancellationToken);

    public Task<Bodega?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _bodegas.GetByIdAsync(id, cancellationToken);

    public async Task<ServiceResult> CreateAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var validated = await ValidateNombreAsync(nombre, excludeId: null, cancellationToken);
        if (validated.Error is not null)
            return ServiceResult.Fail(validated.Error);

        await _bodegas.AddAsync(new Bodega { Nombre = validated.Nombre }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Bodega «{validated.Nombre}» creada.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, string nombre, CancellationToken cancellationToken = default)
    {
        var bodega = await _bodegas.GetByIdAsync(id, cancellationToken);
        if (bodega is null)
            return ServiceResult.Fail("Bodega no encontrada.");

        var validated = await ValidateNombreAsync(nombre, excludeId: id, cancellationToken);
        if (validated.Error is not null)
            return ServiceResult.Fail(validated.Error);

        bodega.Nombre = validated.Nombre;
        _bodegas.Update(bodega);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Bodega «{validated.Nombre}» actualizada.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var bodega = await _bodegas.GetByIdAsync(id, cancellationToken);
        if (bodega is null)
            return ServiceResult.Fail("Bodega no encontrada.");

        if (id == DefaultBodegaId)
            return ServiceResult.Fail(
                "No se puede eliminar Bodega 1: el sistema la usa como bodega por defecto al crear solicitudes de insumos libres.");

        if (await _bodegas.CountAsync(cancellationToken) <= 1)
            return ServiceResult.Fail("No se puede eliminar la última bodega del sistema.");

        var deps = await _bodegas.CountDependenciasAsync(id, cancellationToken);
        if (deps.Any)
        {
            var partes = new List<string>();
            if (deps.Materiales > 0)
                partes.Add($"{deps.Materiales} material(es)");
            if (deps.Solicitudes > 0)
                partes.Add($"{deps.Solicitudes} solicitud(es)");
            if (deps.Bodegueros > 0)
                partes.Add($"{deps.Bodegueros} bodeguero(s)");
            return ServiceResult.Fail(
                $"No se puede eliminar «{bodega.Nombre}»: tiene {string.Join(", ", partes)} asociados. Reasígnelos o elimínelos antes.");
        }

        _bodegas.Remove(bodega);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Bodega «{bodega.Nombre}» eliminada.");
    }

    private async Task<(string Nombre, string? Error)> ValidateNombreAsync(
        string nombre,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return ("", "El nombre de la bodega es obligatorio.");

        var trimmed = nombre.Trim();
        if (trimmed.Length > MaxNombreLength)
            return ("", $"El nombre no puede superar {MaxNombreLength} caracteres.");

        if (await _bodegas.ExistsByNombreAsync(trimmed, cancellationToken, excludeId))
            return ("", "Ya existe una bodega con ese nombre.");

        return (trimmed, null);
    }
}
