using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IGrupoConfeccionService
{
    Task<IReadOnlyList<GrupoConfeccionDto>> GetByOrderAsync(int productionOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrupoConfeccionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAsync(CreateGrupoConfeccionDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> CerrarAsync(int grupoId, TimeOnly horaFin, int cantidadPrendas, CancellationToken cancellationToken = default);
    Task<GrupoConsumoCruzadoDto?> GetConsumosCruzadosAsync(int grupoId, CancellationToken cancellationToken = default);
}
