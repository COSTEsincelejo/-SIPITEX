using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

public class MenuRouteTests
{
    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(relativePath);
    }

    [Fact]
    public void Layout_LinksMatchPluralPlantasInventarioControllers()
    {
        var layout = File.ReadAllText(FindRepoFile(Path.Combine("src", "Sipitex.Web", "Views", "Shared", "_Layout.cshtml")));

        Assert.Contains("asp-controller=\"PlantasInventarioSolicitudes\"", layout, StringComparison.Ordinal);
        Assert.Contains("asp-controller=\"PlantasInventarioOrdenes\"", layout, StringComparison.Ordinal);
        Assert.Contains("Plantas de inventario", layout, StringComparison.Ordinal);
        Assert.Contains("User.IsInRole(\"Instructor\")", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Inventario por bodega", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-action=\"Consultar\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("> Inventario</a>", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"Inventario\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"PlantaInventarioSolicitudes\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-controller=\"PlantaInventarioOrdenes\"", layout, StringComparison.Ordinal);

        Assert.Equal("PlantasInventarioSolicitudesController", typeof(PlantasInventarioSolicitudesController).Name);
        Assert.Equal("PlantasInventarioOrdenesController", typeof(PlantasInventarioOrdenesController).Name);
    }

    [Fact]
    public void SiteJs_SearchUrlsUsePluralPlantasInventarioControllers()
    {
        var js = File.ReadAllText(FindRepoFile(Path.Combine("src", "Sipitex.Web", "wwwroot", "js", "site.js")));
        Assert.Contains("'/PlantasInventarioSolicitudes'", js, StringComparison.Ordinal);
        Assert.Contains("'/PlantasInventarioOrdenes'", js, StringComparison.Ordinal);
        Assert.Contains("'/PlantasInventario/Movimientos'", js, StringComparison.Ordinal);
        Assert.Contains("'/PlantasInventario'", js, StringComparison.Ordinal);
        Assert.DoesNotContain("Inventario por bodega", js, StringComparison.Ordinal);
        Assert.DoesNotContain("'/PlantasInventario/Consultar'", js, StringComparison.Ordinal);
        Assert.DoesNotContain("'/Inventario'", js, StringComparison.Ordinal);
        Assert.DoesNotContain("'/Inventario/Movimientos'", js, StringComparison.Ordinal);
        Assert.DoesNotContain("'/PlantaInventarioSolicitudes'", js, StringComparison.Ordinal);
        Assert.DoesNotContain("'/PlantaInventarioOrdenes'", js, StringComparison.Ordinal);
    }
}
