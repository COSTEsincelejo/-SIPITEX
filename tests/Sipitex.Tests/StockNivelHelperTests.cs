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
    [InlineData(0, 0, StockNivel.SinMinimo)]
    [InlineData(4, 0, StockNivel.SinMinimo)]
    [InlineData(4, -1, StockNivel.SinMinimo)]
    public void Classify_MinimoCeroNoEsCritico(decimal stock, decimal min, StockNivel expected)
    {
        Assert.Equal(expected, StockNivelHelper.Classify(stock, min));
    }

    [Fact]
    public void Contar_SumaOkBajoCriticoYSinMinimo()
    {
        var conteos = StockNivelHelper.Contar(
        [
            StockNivelHelper.Classify(10, 5),
            StockNivelHelper.Classify(2, 5),
            StockNivelHelper.Classify(0, 4),
            StockNivelHelper.Classify(0, 0),
            StockNivelHelper.Classify(8, 0)
        ]);

        Assert.Equal(1, conteos.Ok);
        Assert.Equal(1, conteos.Bajo);
        Assert.Equal(1, conteos.Critico);
        Assert.Equal(2, conteos.SinMinimo);
        Assert.Equal(5, conteos.Total);
        Assert.False(StockNivelHelper.RequiereAtencion(StockNivel.SinMinimo));
        Assert.Equal("Sin mínimo definido", StockNivelHelper.Etiqueta(StockNivel.SinMinimo));
    }
}
