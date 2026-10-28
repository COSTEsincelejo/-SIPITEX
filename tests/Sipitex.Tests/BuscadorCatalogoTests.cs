using System.Security.Claims;
using Sipitex.Application.Search;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class BuscadorCatalogoTests
{
    [Theory]
    [InlineData("crear planta", UserRoles.Administrador, "/PlantasInventario#Nombre")]
    [InlineData("ver stock", UserRoles.Administrador, "/PlantasInventario")]
    [InlineData("quiero ver el stock de la bodega", UserRoles.EncargadoDeBodega, "/PlantasInventario")]
    [InlineData("materiales de una orden", UserRoles.Instructor, "/PlantasInventarioOrdenes")]
    [InlineData("nueva solicitud de materiales", UserRoles.Instructor, "/SolicitudesMaterial/SolicitarInsumos")]
    [InlineData("nueva solicitud de materiales", UserRoles.EncargadoDeBodega, "/PlantasInventarioSolicitudes")]
    [InlineData("reingreso", UserRoles.EncargadoDeBodega, "/PlantasInventarioOrdenes/Reingreso")]
    [InlineData("reingeso", UserRoles.EncargadoDeBodega, "/PlantasInventarioOrdenes/Reingreso")]
    [InlineData("movimientos", UserRoles.Administrador, "/PlantasInventario/Movimientos")]
    [InlineData("inventario bodega", UserRoles.Instructor, "/PlantasInventario")]
    [InlineData("crear orden", UserRoles.Administrador, "/Ordenes#CreateOrder_ProductName")]
    public void Buscar_LlevaALaPantallaDelMenu(string consulta, string rol, string url)
    {
        var resultados = BuscadorCatalogo.Buscar(consulta, Principal(rol));

        Assert.NotEmpty(resultados);
        Assert.Equal(url, resultados[0].Url);
        Assert.InRange(resultados.Count, 1, BuscadorCatalogo.MaximoResultados);
    }

    [Fact]
    public void Buscar_RespetaElRol_YNoPasaDeSeis()
    {
        var instructor = Principal(UserRoles.Instructor);
        var encargado = Principal(UserRoles.EncargadoDeBodega);

        Assert.DoesNotContain(BuscadorCatalogo.Buscar("crear planta", instructor), a => a.Url.Contains("#Nombre", StringComparison.Ordinal));
        Assert.DoesNotContain(BuscadorCatalogo.Buscar("auditoria", encargado), a => a.Url.Contains("/Auditoria", StringComparison.Ordinal));
        Assert.DoesNotContain(BuscadorCatalogo.Buscar("usuarios", instructor), a => a.Url.Contains("/Account/Users", StringComparison.Ordinal));
        Assert.DoesNotContain(BuscadorCatalogo.Buscar("reingreso", instructor), a => a.Url.Contains("Reingreso", StringComparison.Ordinal));
        Assert.DoesNotContain(BuscadorCatalogo.Buscar("movimientos", instructor), a => a.Url.Contains("Movimientos", StringComparison.Ordinal));
        Assert.DoesNotContain(BuscadorCatalogo.Buscar("crear orden", instructor), a => a.Url.Contains("CreateOrder", StringComparison.Ordinal));
        Assert.DoesNotContain(BuscadorCatalogo.JsonVisibles(instructor), "/PlantasInventario#Nombre", StringComparison.Ordinal);
        Assert.Contains("\"/PlantasInventario\"", BuscadorCatalogo.JsonVisibles(instructor), StringComparison.Ordinal);
        Assert.DoesNotContain("/PlantasInventario/Consultar", BuscadorCatalogo.JsonVisibles(instructor), StringComparison.Ordinal);

        Assert.True(BuscadorCatalogo.Buscar("material", Principal(UserRoles.Administrador)).Count <= 6);
        Assert.InRange(BuscadorCatalogo.Frecuentes(Principal(UserRoles.Administrador)).Count, 1, 6);
        Assert.DoesNotContain(BuscadorCatalogo.Frecuentes(instructor), a => a.Url.Contains("Reingreso", StringComparison.Ordinal));
    }

    [Fact]
    public void Buscar_TextoVacio_NoDevuelveNada()
    {
        Assert.Empty(BuscadorCatalogo.Buscar(" ", Principal(UserRoles.Administrador)));
        Assert.Empty(BuscadorCatalogo.Buscar("a", Principal(UserRoles.Administrador)));
    }

    private static ClaimsPrincipal Principal(string rol)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim(ClaimTypes.Role, rol),
            new Claim(ClaimTypes.Name, "Usuario")
        ], "Test");
        return new ClaimsPrincipal(identity);
    }
}
