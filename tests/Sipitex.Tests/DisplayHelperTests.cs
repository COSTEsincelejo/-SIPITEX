using Sipitex.Domain.Enums;
using Sipitex.Web.Helpers;

namespace Sipitex.Tests;

public class DisplayHelperTests
{
    [Fact]
    public void BadgeClass_StockNivel_UsaLosMismosColoresQueInventario()
    {
        Assert.Equal("badge-success", DisplayHelper.BadgeClass(StockNivel.Ok));
        Assert.Equal("badge-warning", DisplayHelper.BadgeClass(StockNivel.Bajo));
        Assert.Equal("badge-danger", DisplayHelper.BadgeClass(StockNivel.Critico));
        Assert.Equal("OK", DisplayHelper.StatusText(StockNivel.Ok));
        Assert.Equal("Bajo", DisplayHelper.StatusText(StockNivel.Bajo));
        Assert.Equal("Crítico", DisplayHelper.StatusText(StockNivel.Critico));
    }

    [Fact]
    public void BadgeClass_StockBajoAlerta_UsaBadgeDeNivelBajo()
    {
        Assert.Equal(
            DisplayHelper.BadgeClass(StockNivel.Bajo),
            DisplayHelper.BadgeClass(AlertType.StockBajo));
    }
}
