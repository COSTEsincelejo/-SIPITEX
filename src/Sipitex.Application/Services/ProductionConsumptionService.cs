using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Descuenta materiales del inventario cuando se registra producción (camino NoAplica).
// Cada descuento deja un StockMovement tipo Salida, igual que la entrega de bodega.
// Si la orden ya pasó por entrega (MaterialsStatus != NoAplica), ProductionOrderService
// no llama a este servicio: MaterialConsumptionService solo registra uso sin tocar stock.
public class ProductionConsumptionService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly IStockMovementRepository _stockMovements;

    public ProductionConsumptionService(
        IBomRepository bomRepository,
        IMaterialRepository materialRepository,
        IStockMovementRepository stockMovements)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
        _stockMovements = stockMovements;
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
    public async Task<bool> ConsumeAsync(
        string productName,
        int units,
        int orderId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        return await ConsumeRecipeAsync(
            recipe.Select(i => new RecipeLine(i.MaterialId, i.QuantityPerUnit)).ToList(),
            units,
            orderId,
            usuarioId,
            cancellationToken);
    }

    // Descuenta según receta explícita (snapshot de la orden) y registra StockMovement por cada material.
    // Debe invocarse dentro de la misma transacción que confirma el avance de la orden.
    public async Task<bool> ConsumeRecipeAsync(
        IReadOnlyList<RecipeLine> recipe,
        int units,
        int orderId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (recipe.Count == 0) return false;
        if (orderId <= 0 || usuarioId <= 0) return false;

        foreach (var item in recipe)
        {
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            if (material is null || material.Stock < item.QuantityPerUnit * units)
                return false;
        }

        var movements = new List<StockMovement>(recipe.Count);
        foreach (var item in recipe)
        {
            var material = (await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken))!;
            var consumo = item.QuantityPerUnit * units;
            material.DescontarUnidadesSinCosto(consumo);
            material.Stock -= consumo;
            _materialRepository.Update(material);

            movements.Add(new StockMovement
            {
                MaterialId = material.Id,
                FechaUtc = DateTime.UtcNow,
                UsuarioId = usuarioId,
                TipoMovimiento = StockMovementType.Salida,
                Cantidad = consumo,
                StockResultante = material.Stock,
                Referencia = $"Orden:{orderId}",
                CostoUnitario = material.CostoPromedioPonderado > 0
                    ? material.CostoPromedioPonderado
                    : null
            });
        }

        if (movements.Count > 0)
            await _stockMovements.AddRangeAsync(movements, cancellationToken);

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
