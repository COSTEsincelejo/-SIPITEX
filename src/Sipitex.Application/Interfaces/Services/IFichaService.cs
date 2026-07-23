using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IFichaService
{
    Task<IReadOnlyList<FichaDto>> GetFichasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionSessionDto>> GetRecentSessionsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> RegisterSessionAsync(RegisterProductionDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> QuickRegisterAsync(int fichaId, int units, string? observations = null, CancellationToken cancellationToken = default);
}
