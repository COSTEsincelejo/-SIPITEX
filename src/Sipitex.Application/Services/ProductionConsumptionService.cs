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
        // Receta del producto en el BOM
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        foreach (var item in recipe)
        {
            // Cargo el material de cada línea del BOM
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            // Si falta material o no alcanza stock, no se puede consumir
            if (material is null || material.Stock < item.QuantityPerUnit * units)
                return false;
        }
        // También tiene que haber receta (producto válido en BOM)
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

        // Si pasó validación, ahora sí descuento cada material
        foreach (var item in recipe)
        {
            // Vuelvo a cargar porque EF puede haber trackeado la entidad
            var material = (await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken))!;
            // Esto descuenta según cantidad por unidad × unidades producidas
            material.Stock -= item.QuantityPerUnit * units;
            _materialRepository.Update(material);
        }

        return true;
    }

    // Suma unidades producidas y cierra la orden si ya llegó a la meta
    public static void UpdateOrderProgress(ProductionOrder order, int units)
    {
        // Sumo lo producido en esta operación
        order.ProducedQuantity += units;
        // Si ya cumplió la meta, la orden queda finalizada
        if (order.ProducedQuantity >= order.TotalQuantity)
            order.Status = OrderStatus.Finalizada;
    }
}
