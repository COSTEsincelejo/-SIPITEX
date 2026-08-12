using Sipitex.Domain.Enums;

namespace Sipitex.Domain;

// Regla única de nivel de stock de inventario (Stock + MinStock).
// Critico = Stock <= 0 (siempre, aunque MinStock sea 0).
// Bajo = Stock > 0 && Stock < MinStock.
// IsLowStock (atención) = Critico || Bajo.
public static class StockLevelRules
{
    public static StockLevel Resolve(decimal stock, decimal minStock)
    {
        if (stock <= 0)
            return StockLevel.Critico;
        if (stock < minStock)
            return StockLevel.Bajo;
        return StockLevel.Ok;
    }

    // True si requiere atención (mismo universo que alertas/KPI de stock).
    public static bool IsLowStock(decimal stock, decimal minStock) =>
        Resolve(stock, minStock) is StockLevel.Bajo or StockLevel.Critico;

    public static bool IsCritical(decimal stock, decimal minStock) =>
        Resolve(stock, minStock) == StockLevel.Critico;

    public static bool IsBelowMinimum(decimal stock, decimal minStock) =>
        Resolve(stock, minStock) == StockLevel.Bajo;
}
