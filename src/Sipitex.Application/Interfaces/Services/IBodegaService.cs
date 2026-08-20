using Sipitex.Application.DTOs;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Services;

// Catálogo de bodegas: listar, crear, editar y borrar (solo Administrador)
public interface IBodegaService
{
    Task<IReadOnlyList<Bodega>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Bodega?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(string nombre, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAsync(int id, string nombre, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
