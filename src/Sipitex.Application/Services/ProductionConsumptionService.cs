using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Descuenta materiales del inventario cuando se registra producción (según BOM)
public class ProductionConsumptionService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;

    public ProductionConsumptionService(IBomRepository bomRepository, IMaterialRepository materialRepository)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
    }

    // Solo revisa si alcanza el stock, no modifica nada
    public async Task<bool> CanConsumeAsync(string productName, int units, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        foreach (var item in recipe)
        {
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            if (material is null || material.Stock < item.QuantityPerUnit * units)
                return false;
        }
        return recipe.Count > 0;
    }

    // Descuenta materiales; devuelve false si no alcanza o no hay receta
    public async Task<bool> ConsumeAsync(string productName, int units, CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        if (recipe.Count == 0) return false;

        // Primero valido todo para no dejar stock a medias
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
