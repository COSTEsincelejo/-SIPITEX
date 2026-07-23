using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Interfaces.Services;

public interface IMrpService
{
    Task<IReadOnlyList<BomItemDto>> GetBomAsync(CancellationToken cancellationToken = default);
    Task<MrpSimulationResultDto> SimulateAsync(string productName, decimal quantity, CancellationToken cancellationToken = default);
    Task<bool> ProductHasBomAsync(string productName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetKnownProductNamesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> AddBomItemAsync(string productName, string materialName, decimal quantityPerUnit, MaterialUnit unit = MaterialUnit.Metros, CancellationToken cancellationToken = default);
}
