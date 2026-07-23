using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class ProductionConsumptionService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;

    public ProductionConsumptionService(IBomRepository bomRepository, IMaterialRepository materialRepository)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
    }

    public async Task<bool> CanConsumeAsync(string productName, int units, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        // Sin ficha BOM: se permite producir (sin descontar materiales).
        if (recipe.Count == 0) return true;

        foreach (var item in recipe)
        {
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            if (material is null || material.Stock < item.QuantityPerUnit * units)
                return false;
        }
        return true;
    }

    public async Task<bool> ConsumeAsync(string productName, int units, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        // Producto libre sin BOM: no descuenta stock, pero la producción sí se registra.
        if (recipe.Count == 0) return true;

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

    public static void UpdateOrderProgress(ProductionOrder order, int units)
    {
        order.ProducedQuantity += units;
        if (order.ProducedQuantity >= order.TotalQuantity)
            order.Status = OrderStatus.Finalizada;
    }
}
