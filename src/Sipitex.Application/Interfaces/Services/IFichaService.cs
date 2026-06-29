using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IFichaService
{
    Task<IReadOnlyList<FichaDto>> GetFichasAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> RegisterSessionAsync(RegisterProductionDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> QuickRegisterAsync(int fichaId, int units, CancellationToken cancellationToken = default);
}
