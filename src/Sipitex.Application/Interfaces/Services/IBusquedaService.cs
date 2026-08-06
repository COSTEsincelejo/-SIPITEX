using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Búsqueda ligera de entidades para el header (máx. 5 por categoría)
public interface IBusquedaService
{
    Task<IReadOnlyList<BusquedaItemDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
