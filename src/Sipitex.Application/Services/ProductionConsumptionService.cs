using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Descuenta materiales del inventario cuando se registra producción
public class ProductionConsumptionService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;

    public ProductionConsumptionService(IBomRepository bomRepository, IMaterialRepository materialRepository)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
    }

    // Línea de receta genérica (snapshot o BOM vivo)
    public readonly record struct RecipeLine(int MaterialId, decimal QuantityPerUnit);

    // Solo revisa si alcanza el stock, no modifica nada
    public async Task<bool> CanConsumeAsync(string productName, int units, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        return await CanConsumeRecipeAsync(
            recipe.Select(i => new RecipeLine(i.MaterialId, i.QuantityPerUnit)).ToList(),
            units,
            cancellationToken);
    }

    public async Task<bool> CanConsumeRecipeAsync(IReadOnlyList<RecipeLine> recipe, int units, CancellationToken cancellationToken = default)
    {
        if (recipe.Count == 0) return false;
        foreach (var item in recipe)
        {
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            if (material is null || material.Stock < item.QuantityPerUnit * units)
                return false;
        }
        return true;
    }

    // Descuenta materiales según BOM vivo del producto (legado / simulación)
    public async Task<bool> ConsumeAsync(string productName, int units, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        return await ConsumeRecipeAsync(
            recipe.Select(i => new RecipeLine(i.MaterialId, i.QuantityPerUnit)).ToList(),
            units,
            cancellationToken);
    }

    // Descuenta según receta explícita (snapshot de la orden)
    public async Task<bool> ConsumeRecipeAsync(IReadOnlyList<RecipeLine> recipe, int units, CancellationToken cancellationToken = default)
    {
        if (recipe.Count == 0) return false;

        foreach (var item in recipe)
        {
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            if (material is null || material.Stock < item.QuantityPerUnit * units)
                return false;
        }

        foreach (var item in recipe)
        {
            var material = (await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken))!;
            material.Stock -= item.QuantityPerUnit * units;
            _materialRepository.Update(material);
        }

        return true;
    }

    // Suma unidades producidas y cierra la orden si ya llegó a la meta
    public static void UpdateOrderProgress(ProductionOrder order, int units)
    {
        order.ProducedQuantity += units;
        if (order.ProducedQuantity >= order.TotalQuantity)
            order.Status = OrderStatus.Finalizada;
    }
}
