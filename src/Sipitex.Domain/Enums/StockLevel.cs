namespace Sipitex.Domain.Enums;

// Nivel de stock de inventario general (no confundir con MaterialStockAvailability de líneas de orden).
// Critico + Bajo = IsLowStock (atención requerida).
public enum StockLevel
{
    Ok = 0,
    Bajo = 1,      // Stock > 0 && Stock < MinStock
    Critico = 2    // Stock <= 0 (sin existencias; incluye borde Stock=0 / MinStock=0)
}
