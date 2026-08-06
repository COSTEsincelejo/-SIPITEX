using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

// Convierte unidades del enum a texto corto para la UI
public static class UnitHelper
{
    // Acá mapeo cada unidad a su abreviatura en pantalla
    public static string ToDisplay(MaterialUnit unit) => unit switch
    {
        MaterialUnit.Metros => "m",
        MaterialUnit.Unidades => "ud",
        MaterialUnit.Kg => "kg",
        MaterialUnit.Gramos => "g",
        _ => unit.ToString()
    };
}
