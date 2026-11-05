using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

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
        string? categoria,
        string? categoriaPlantilla = null,
        TipoElemento? tipoElemento = null)
    {
        IEnumerable<MaterialPlantaStockDto> query = items ?? [];

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.Trim();
            query = query.Where(m =>
                m.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || m.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                || Contains(m.CodigoAlmacen, term)
                || Contains(m.Referencia, term)
                || Contains(m.Categoria, term));
        }

        if (EsMateriales(categoria))
            query = query.Where(m => m.EnFichaTecnica);
        else if (EsInsumos(categoria))
            query = query.Where(m => !m.EnFichaTecnica);

        if (!string.IsNullOrWhiteSpace(categoriaPlantilla))
        {
            var cat = categoriaPlantilla.Trim();
            query = query.Where(m =>
                !string.IsNullOrWhiteSpace(m.Categoria)
                && string.Equals(m.Categoria.Trim(), cat, StringComparison.OrdinalIgnoreCase));
        }

        if (tipoElemento is TipoElemento tipo)
            query = query.Where(m => m.TipoElemento == tipo);

        return query
            .OrderBy(m => m.EnFichaTecnica ? 0 : 1)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static bool Coincide(string? categoria, params string[] esperados)
    {
        if (string.IsNullOrWhiteSpace(categoria))
            return false;

        var value = categoria.Trim();
        return esperados.Any(e => string.Equals(value, e, StringComparison.OrdinalIgnoreCase));
    }
}
