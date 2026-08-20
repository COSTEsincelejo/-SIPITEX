using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Alta y consulta de bodegas. Nombre único (case-insensitive).
public class BodegaService : IBodegaService
{
    private const int MaxNombreLength = 80;

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
        if (string.IsNullOrWhiteSpace(nombre))
            return ServiceResult.Fail("El nombre de la bodega es obligatorio.");

        var trimmed = nombre.Trim();
        if (trimmed.Length > MaxNombreLength)
            return ServiceResult.Fail($"El nombre no puede superar {MaxNombreLength} caracteres.");

        if (await _bodegas.ExistsByNombreAsync(trimmed, cancellationToken))
            return ServiceResult.Fail("Ya existe una bodega con ese nombre.");

        await _bodegas.AddAsync(new Bodega { Nombre = trimmed }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Bodega «{trimmed}» creada.");
    }
}
