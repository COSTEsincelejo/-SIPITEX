using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

// Traduce el texto de la plantilla CMTC a MaterialUnit. No adivina plurales ni sinónimos fuera de la lista.
public static class MaterialUnitImportParser
{
    public readonly record struct Resultado(bool Reconocida, bool Vacia, MaterialUnit Unidad)
    {
        public static Resultado SinValor() => new(false, true, default);
        public static Resultado Desconocida() => new(false, false, default);
        public static Resultado Ok(MaterialUnit unidad) => new(true, false, unidad);
    }

    public static Resultado Parse(string? raw)
    {
        var key = TextoNormalizado.Clave(raw);
        if (key.Length == 0)
            return Resultado.SinValor();

        // Alias explícitos, antes que el nombre del enum: "und" es Unidades, no Und.
        var alias = key switch
        {
            "metro" or "m" or "metros" => (MaterialUnit?)MaterialUnit.Metros,
            "kilo" or "kg" => (MaterialUnit?)MaterialUnit.Kg,
            "unidad" or "und" or "unidades" => (MaterialUnit?)MaterialUnit.Unidades,
            "paquet" or "paquete" => (MaterialUnit?)MaterialUnit.Paquete,
            "pano" => (MaterialUnit?)MaterialUnit.Pano,
            "gruesa" => (MaterialUnit?)MaterialUnit.Gruesa,
            "dm" or "decimetro" => (MaterialUnit?)MaterialUnit.Decimetro,
            "caja" => (MaterialUnit?)MaterialUnit.Caja,
            "libra" => (MaterialUnit?)MaterialUnit.Libra,
            "rollo" => (MaterialUnit?)MaterialUnit.Rollo,
            "par" => (MaterialUnit?)MaterialUnit.Par,
            "cono" => (MaterialUnit?)MaterialUnit.Cono,
            "bolsa" => (MaterialUnit?)MaterialUnit.Bolsa,
            "bulto" => (MaterialUnit?)MaterialUnit.Bulto,
            "juego" => (MaterialUnit?)MaterialUnit.Juego,
            _ => null
        };
        if (alias is MaterialUnit porAlias)
            return Resultado.Ok(porAlias);

        foreach (var unit in UnitHelper.Catalog)
        {
            if (TextoNormalizado.Clave(unit.ToString()) == key)
                return Resultado.Ok(unit);
        }

        foreach (var unit in UnitHelper.Catalog)
        {
            if (TextoNormalizado.Clave(UnitHelper.ToDisplay(unit)) == key)
                return Resultado.Ok(unit);
        }

        return Resultado.Desconocida();
    }
}
