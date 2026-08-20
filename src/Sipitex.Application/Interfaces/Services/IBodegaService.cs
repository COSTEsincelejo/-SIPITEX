using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Services;

// Catálogo de bodegas: listar y crear (sin edición/borrado en este PR)
public interface IBodegaService
{
    Task<IReadOnlyList<Bodega>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Bodega?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(string nombre, CancellationToken cancellationToken = default);
}
