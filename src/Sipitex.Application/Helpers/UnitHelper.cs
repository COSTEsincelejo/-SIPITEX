using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

// Convierte unidades del enum a texto para la UI y catálogo 2.3
public static class UnitHelper
{
    public static string ToDisplay(MaterialUnit unit) => unit switch
    {
        MaterialUnit.Metros => "metro",
        MaterialUnit.Unidades => "unidad",
        MaterialUnit.Kg => "kilogramo",
        MaterialUnit.Gramos => "gramo",
        MaterialUnit.Galon => "galón",
        MaterialUnit.Caja => "caja",
        MaterialUnit.Uni => "UNI",
        MaterialUnit.Und => "UND",
        MaterialUnit.Docena => "docena",
        MaterialUnit.Decena => "decena",
        MaterialUnit.Litro => "litro",
        MaterialUnit.Libra => "libra",
        MaterialUnit.Yarda => "yarda",
        _ => unit.ToString()
    };

    public static IReadOnlyList<MaterialUnit> Catalog { get; } = Enum.GetValues<MaterialUnit>();
}
