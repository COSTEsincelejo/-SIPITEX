using Sipitex.Application.Helpers;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class StockNivelHelperTests
{
    [Theory]
    [InlineData(10, 5, StockNivel.Ok)]
    [InlineData(5, 5, StockNivel.Ok)]
    [InlineData(2, 5, StockNivel.Bajo)]
    [InlineData(0, 5, StockNivel.Critico)]
    [InlineData(-1, 5, StockNivel.Critico)]
    [InlineData(4, 0, StockNivel.Ok)]
    public void Classify_TresNiveles(decimal stock, decimal min, StockNivel expected)
    {
        Assert.Equal(expected, StockNivelHelper.Classify(stock, min));
    }
}
