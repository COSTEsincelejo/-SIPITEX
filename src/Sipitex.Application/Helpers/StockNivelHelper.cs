using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

// Única regla de nivel. MinStock <= 0 no es una falta: es "Sin mínimo definido".
// Con mínimo definido: Crítico = sin existencias; Bajo = hay stock pero no llega al mínimo; OK = stock >= mínimo.
public static class StockNivelHelper
{
    public static StockNivel Classify(decimal stock, decimal minStock)
    {
        if (minStock <= 0)
            return StockNivel.SinMinimo;

        if (stock <= 0)
            return StockNivel.Critico;

        if (stock < minStock)
            return StockNivel.Bajo;

        return StockNivel.Ok;
    }

    public static bool RequiereAtencion(StockNivel nivel) =>
        nivel is StockNivel.Bajo or StockNivel.Critico;

    public static string Etiqueta(StockNivel nivel) => nivel switch
    {
        StockNivel.Ok => "OK",
        StockNivel.Bajo => "Bajo",
        StockNivel.Critico => "Crítico",
        StockNivel.SinMinimo => "Sin mínimo definido",
        _ => nivel.ToString()
    };

    public static StockNivelConteos Contar(IEnumerable<StockNivel> niveles)
    {
        var ok = 0;
        var bajo = 0;
        var critico = 0;
        var sinMinimo = 0;
        foreach (var nivel in niveles)
        {
            switch (nivel)
            {
                case StockNivel.Ok:
                    ok++;
                    break;
                case StockNivel.Bajo:
                    bajo++;
                    break;
                case StockNivel.Critico:
                    critico++;
                    break;
                case StockNivel.SinMinimo:
                    sinMinimo++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(niveles), nivel, "Nivel de stock no contemplado.");
            }
        }

        return new StockNivelConteos(ok, bajo, critico, sinMinimo);
    }
}

public readonly record struct StockNivelConteos(int Ok, int Bajo, int Critico, int SinMinimo)
{
    public int Total => Ok + Bajo + Critico + SinMinimo;
}
