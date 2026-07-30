using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Lista BOM y simulación de necesidades de materiales
public interface IMrpService
{
    Task<IReadOnlyList<BomItemDto>> GetBomAsync(CancellationToken cancellationToken = default);
    // quantity = unidades a producir para la simulación
    Task<MrpSimulationResultDto> SimulateAsync(string productName, decimal quantity, CancellationToken cancellationToken = default);
    // Validar que el producto tenga receta antes de crear orden
    Task<bool> ProductHasBomAsync(string productName, CancellationToken cancellationToken = default);
}
