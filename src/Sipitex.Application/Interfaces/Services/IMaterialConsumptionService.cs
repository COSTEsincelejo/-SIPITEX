using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Promedio ponderado de costo de materiales consumidos (cantidad × costo de adquisición).
public interface IMaterialConsumptionCostService
{
    decimal CalcularPromedioPonderado(IReadOnlyList<ConsumoCostoLineaDto> lineas);
}

public interface IMaterialConsumptionService
{
    Task<IReadOnlyList<ConsumoMaterialDto>> GetByOrderAsync(int productionOrderId, CancellationToken cancellationToken = default);
    Task<ServiceResult> RegisterAsync(RegisterConsumoMaterialDto dto, CancellationToken cancellationToken = default);
    Task<ConsumoCostoResumenDto> GetCostoPromedioByOrderAsync(int productionOrderId, CancellationToken cancellationToken = default);
}
