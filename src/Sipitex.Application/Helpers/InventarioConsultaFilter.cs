using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

// Filtro de la consulta de inventario (nombre y nivel OK/Bajo/Crítico/Sin mínimo).
public static class InventarioConsultaFilter
{
    public const string Faltantes = "Faltantes";

    public static IReadOnlyList<MaterialDto> Apply(
        IReadOnlyList<MaterialDto> materials,
        string? nombre,
        string? nivel,
        string? categoria = null,
        TipoElemento? tipoElemento = null)
    {
        if (materials is null || materials.Count == 0)
            return materials ?? [];

        IEnumerable<MaterialDto> query = materials;
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var term = nombre.Trim();
            query = query.Where(m =>
                m.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || Contains(m.CodigoAlmacen, term)
                || Contains(m.Referencia, term));
        }

        if (!string.IsNullOrWhiteSpace(nivel))
            query = query.Where(m => MatchesNivel(m, nivel));

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var cat = categoria.Trim();
            query = query.Where(m =>
                !string.IsNullOrWhiteSpace(m.Categoria)
                && string.Equals(m.Categoria.Trim(), cat, StringComparison.OrdinalIgnoreCase));
        }

        if (tipoElemento is TipoElemento tipo)
            query = query.Where(m => m.TipoElemento == tipo);

        return query.ToList();
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(term, StringComparison.OrdinalIgnoreCase);

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
