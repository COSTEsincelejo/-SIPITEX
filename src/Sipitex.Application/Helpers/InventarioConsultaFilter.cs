using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

// Filtro de la consulta de inventario por bodega (nombre y nivel OK/Bajo/Crítico).
public static class InventarioConsultaFilter
{
    public const string Faltantes = "Faltantes";

    public static IReadOnlyList<MaterialDto> Apply(
        IReadOnlyList<MaterialDto> materials,
        string? nombre,
        string? nivel)
    {
        if (materials is null || materials.Count == 0)
            return materials ?? [];

        IEnumerable<MaterialDto> query = materials;
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var term = nombre.Trim();
            query = query.Where(m => m.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(nivel))
            query = query.Where(m => MatchesNivel(m, nivel));

        return query.ToList();
    }

    public static bool MatchesNivel(MaterialDto material, string? nivel)
    {
        if (string.IsNullOrWhiteSpace(nivel))
            return true;

        var actual = StockNivelHelper.Classify(material.Stock, material.MinStock);
        if (string.Equals(nivel, Faltantes, StringComparison.OrdinalIgnoreCase))
            return actual is StockNivel.Bajo or StockNivel.Critico;

        return Enum.TryParse<StockNivel>(nivel, ignoreCase: true, out var parsed) && actual == parsed;
    }
}
