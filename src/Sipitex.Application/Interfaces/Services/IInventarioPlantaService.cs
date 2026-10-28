using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Consulta de stock por planta. Respeta las plantas del accessor (encargado de bodega).
public interface IInventarioPlantaService
{
    Task<InventarioPlantaConsultaDto> ConsultarAsync(
        int? plantaId,
        string? q,
        string? nivel,
        CancellationToken cancellationToken = default);
}
