using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

// MRP: lista de materiales (BOM) y simulación de necesidades
public class MrpService : IMrpService
{
    private readonly IBomRepository _bomRepository;
    private readonly IMaterialRepository _materialRepository;

    public MrpService(IBomRepository bomRepository, IMaterialRepository materialRepository)
    {
        _bomRepository = bomRepository;
        _materialRepository = materialRepository;
    }

    // Toda la lista de materiales por producto
    public async Task<IReadOnlyList<BomItemDto>> GetBomAsync(CancellationToken cancellationToken = default)
    {
        // Con esto traigo todos los ítems del BOM
        var items = await _bomRepository.GetAllAsync(cancellationToken);
        // Los paso a DTO con unidad legible
        return items.Select(i => new BomItemDto(
            i.ProductName,
            i.Material.Name,
            i.QuantityPerUnit,
            UnitHelper.ToDisplay(i.Unit))).ToList();
    }

    // Simula cuánto material haría falta para producir X unidades de un producto
    public async Task<MrpSimulationResultDto> SimulateAsync(string productName, decimal quantity, CancellationToken cancellationToken = default)
    {
        // Receta del producto en el BOM
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        // Acá voy armando cada línea del resultado
        var lines = new List<MrpLineDto>();

        // Por cada material de la receta calculo requerido vs disponible
        foreach (var item in recipe)
        {
            // Stock actual del material en bodega
            var material = await _materialRepository.GetByIdAsync(item.MaterialId, cancellationToken);
            // Stock actual en bodega (0 si no existe el material)
            var available = material?.Stock ?? 0;
            // Cantidad total que se necesita para las unidades pedidas
            var required = item.QuantityPerUnit * quantity;
            // Lo que falta (0 si sobra stock)
            var deficit = Math.Max(0, required - available);

            // Una fila de la tabla de simulación
            lines.Add(new MrpLineDto(
                item.Material.Name,
                required,
                available,
                deficit,
                UnitHelper.ToDisplay(item.Unit),
                deficit <= 0));
        }

        // Devuelvo producto, cantidad pedida y todas las líneas
        return new MrpSimulationResultDto(productName, quantity, lines);
    }

    // Para validar en el formulario de órdenes que el producto exista en el BOM
    public async Task<bool> ProductHasBomAsync(string productName, CancellationToken cancellationToken = default)
    {
        // Busco si hay receta para ese nombre de producto
        var recipe = await _bomRepository.GetByProductAsync(productName, cancellationToken);
        // True si tiene al menos un material en la receta
        return recipe.Count > 0;
    }
}
