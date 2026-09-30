using Sipitex.Application.DTOs;

namespace Sipitex.Application.Helpers;

// Búsqueda y categoría del detalle de una planta.
// No hay columna de categoría: Material = el ítem está en alguna ficha técnica (BomItem);
// Insumo = está en la planta y no figura en ninguna ficha. El nivel de stock sigue en StockNivelHelper.
public static class PlantaDetalleConsulta
{
    public const string Materiales = "materiales";
    public const string Insumos = "insumos";
    public const string EtiquetaMaterial = "Material";
    public const string EtiquetaInsumo = "Insumo";

    public static string Etiqueta(bool enFichaTecnica) =>
        enFichaTecnica ? EtiquetaMaterial : EtiquetaInsumo;

    public static bool EsMateriales(string? categoria) =>
        Coincide(categoria, Materiales, "material");

    public static bool EsInsumos(string? categoria) =>
        Coincide(categoria, Insumos, "insumo");

    public static IReadOnlyList<MaterialPlantaStockDto> Apply(
        IReadOnlyList<MaterialPlantaStockDto> items,
        string? busqueda,
        string? categoria)
    {
        IEnumerable<MaterialPlantaStockDto> query = items ?? [];

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.Trim();
            query = query.Where(m =>
                m.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || m.Code.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (EsMateriales(categoria))
            query = query.Where(m => m.EnFichaTecnica);
        else if (EsInsumos(categoria))
            query = query.Where(m => !m.EnFichaTecnica);

        return query
            .OrderBy(m => m.EnFichaTecnica ? 0 : 1)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool Coincide(string? categoria, params string[] esperados)
    {
        if (string.IsNullOrWhiteSpace(categoria))
            return false;

        var value = categoria.Trim();
        return esperados.Any(e => string.Equals(value, e, StringComparison.OrdinalIgnoreCase));
    }
}
