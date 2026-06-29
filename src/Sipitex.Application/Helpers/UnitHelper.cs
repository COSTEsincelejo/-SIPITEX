using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

public static class UnitHelper
{
    public static string ToDisplay(MaterialUnit unit) => unit switch
    {
        MaterialUnit.Metros => "m",
        MaterialUnit.Unidades => "ud",
        MaterialUnit.Kg => "kg",
        _ => unit.ToString()
    };
}
