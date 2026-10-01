namespace Sipitex.Domain.Enums;

// Nivel de stock respecto del mínimo de reposición. No se persiste: se calcula en StockNivelHelper.
public enum StockNivel
{
    Ok = 0,
    Bajo = 1,
    Critico = 2,
    SinMinimo = 3
}
