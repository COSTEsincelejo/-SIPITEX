using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IGarmentCostingService
{
    Task<GarmentCostDto> CalcularAsync(int productionOrderId, CancellationToken cancellationToken = default);
}
