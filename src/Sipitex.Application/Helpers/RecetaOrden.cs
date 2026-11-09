using System.Globalization;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

// Texto de la receta por unidad. Órdenes y Trazabilidad leen el mismo snapshot (o el BOM vivo)
// y la unidad sale de UnitHelper, no de un sufijo fijo.
public static class RecetaOrden
{
    public static IReadOnlyList<string> LineasTrazabilidad(IEnumerable<ProductionOrderBomSnapshot> snapshots) =>
        snapshots
            .Select(s => Linea(s.MaterialName, s.QuantityPerUnit, s.Unit))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<string> LineasTrazabilidad(IEnumerable<BomItem> items) =>
        items
            .Select(b => Linea(b.Material?.Name ?? $"Material #{b.MaterialId}", b.QuantityPerUnit, b.Unit))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string Linea(string nombre, decimal cantidadPorUnidad, MaterialUnit unidad) =>
        $"{nombre} ({cantidadPorUnidad.ToString("0.##", CultureInfo.InvariantCulture)} / {UnitHelper.ToDisplay(unidad)})";
}
