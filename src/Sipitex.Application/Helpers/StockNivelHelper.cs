using Sipitex.Domain.Enums;

namespace Sipitex.Application.Helpers;

// OK ≥ mínimo; Bajo = hay stock pero bajo el mínimo; Crítico = sin existencias.
public static class StockNivelHelper
{
    public static StockNivel Classify(decimal stock, decimal minStock)
    {
        if (stock <= 0)
            return StockNivel.Critico;

        if (minStock > 0 && stock < minStock)
            return StockNivel.Bajo;

        return StockNivel.Ok;
    }
}

