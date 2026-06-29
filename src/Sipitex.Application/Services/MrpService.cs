using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

public class MrpService : IMrpService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;

    public MrpService(IBomRepository bomRepository, IMaterialRepository materialRepository)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
    }

    public async Task<IReadOnlyList<BomItemDto>> GetBomAsync(CancellationToken cancellationToken = default)
    {
        var items = await _bomRepository.GetAllAsync(cancellationToken);
        return items.Select(i => new BomItemDto(
            i.ProductName,
            i.Material.Name,
            i.QuantityPerUnit,
            UnitHelper.ToDisplay(i.Unit))).ToList();
    }

    public async Task<MrpSimulationResultDto> SimulateAsync(string productName, decimal quantity, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        var lines = new List<MrpLineDto>();

        foreach (var item in recipe)
        {
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            var available = material?.Stock ?? 0;
            var required = item.QuantityPerUnit * quantity;
            var deficit = Math.Max(0, required - available);

            lines.Add(new MrpLineDto(
                item.Material.Name,
                required,
                available,
                deficit,
                UnitHelper.ToDisplay(item.Unit),
                deficit <= 0));
        }

        return new MrpSimulationResultDto(productName, quantity, lines);
    }

    public async Task<bool> ProductHasBomAsync(string productName, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        return recipe.Count > 0;
    }
}
