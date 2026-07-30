using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Lista BOM y simulación de necesidades de materiales
public interface IMrpService
{
    Task<IReadOnlyList<BomItemDto>> GetBomAsync(CancellationToken cancellationToken = default);
    Task<MrpSimulationResultDto> SimulateAsync(string productName, decimal quantity, CancellationToken cancellationToken = default);
    Task<bool> ProductHasBomAsync(string productName, CancellationToken cancellationToken = default);
}
