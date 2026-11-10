using Sipitex.Domain.Entities;

namespace Sipitex.Application.Helpers;

// Una sola cuenta para consumir y para reingresar:
// disponible de la línea = entregado − reingresado − consumido de esa línea.
// Los consumos antiguos sin OrderMaterialRequirementId se restan del total del material.
public static class DisponibilidadMaterialOrden
{
    public static decimal DeLaLinea(ProductionOrderMaterialRequirement linea, decimal consumidoEnLaLinea) =>
        linea.QuantityDelivered - linea.QuantityReentered - consumidoEnLaLinea;

    public static decimal ConsumidoEnLinea(IEnumerable<ConsumoMaterial> consumos, int lineaId) =>
        consumos.Where(c => c.OrderMaterialRequirementId == lineaId).Sum(c => c.Cantidad);

    public static DisponibilidadMaterial DelMaterial(
        IReadOnlyList<ProductionOrderMaterialRequirement> lineas,
        IEnumerable<ConsumoMaterial> consumosDelMaterial)
    {
        var consumos = consumosDelMaterial as IReadOnlyCollection<ConsumoMaterial>
            ?? consumosDelMaterial.ToList();
        var entregado = lineas.Sum(l => l.QuantityDelivered);
        var reingresado = lineas.Sum(l => l.QuantityReentered);
        var consumido = consumos.Sum(c => c.Cantidad);
        var porLinea = lineas.Sum(l => DeLaLinea(l, ConsumidoEnLinea(consumos, l.Id)));
        var ids = lineas.Select(l => l.Id).ToHashSet();
        var sinLinea = consumos
            .Where(c => c.OrderMaterialRequirementId is not int linea || !ids.Contains(linea))
            .Sum(c => c.Cantidad);

        return new DisponibilidadMaterial(entregado, reingresado, consumido, porLinea - sinLinea);
    }
}

public readonly record struct DisponibilidadMaterial(
    decimal Entregado,
    decimal Reingresado,
    decimal Consumido,
    decimal Disponible);
