using System.Security.Claims;
using Sipitex.Application.Ayuda;
using Sipitex.Application.Search;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class AyudaCatalogoTests
{
    [Fact]
    public void Administrador_VeTodosLosTemasPrincipales()
    {
        var ids = Ids(UserRoles.Administrador);

        Assert.Contains("ordenes", ids);
        Assert.Contains("mrp", ids);
        Assert.Contains("plantas", ids);
        Assert.Contains("inventario", ids);
        Assert.Contains("solicitudes", ids);
        Assert.Contains("reingreso", ids);
        Assert.Contains("movimientos", ids);
        Assert.Contains("costeo", ids);
        Assert.Contains("usuarios", ids);
        Assert.Contains("perfil", ids);
    }

    [Fact]
    public void Instructor_VeOrdenesFichasYSolicitudes_NoAdministracionDeBodega()
    {
        var ids = Ids(UserRoles.Instructor);

        Assert.Contains("ordenes", ids);
        Assert.Contains("mrp", ids);
        Assert.Contains("solicitudes", ids);
        Assert.Contains("costeo", ids);
        Assert.Contains("perfil", ids);
        Assert.DoesNotContain("usuarios", ids);
        Assert.DoesNotContain("plantas", ids);
        Assert.DoesNotContain("reingreso", ids);
        Assert.DoesNotContain("movimientos", ids);
    }

    [Fact]
    public void Encargado_VeInventarioYSolicitudes_NoUsuariosNiCosteo()
    {
        var ids = Ids(UserRoles.EncargadoDeBodega);

        Assert.Contains("plantas", ids);
        Assert.Contains("inventario", ids);
        Assert.Contains("solicitudes", ids);
        Assert.Contains("reingreso", ids);
        Assert.Contains("movimientos", ids);
        Assert.Contains("perfil", ids);
        Assert.DoesNotContain("usuarios", ids);
        Assert.DoesNotContain("costeo", ids);
    }

    [Theory]
    [InlineData("cómo crear orden", "/Ayuda#ordenes", "/Ordenes#CreateOrder_ProductName")]
    [InlineData("ayuda", "/Ayuda", null)]
    [InlineData("planta", "/Ayuda#plantas", null)]
    [InlineData("solicitar materiales", "/Ayuda#solicitudes", null)]
    [InlineData("crear usuario", "/Ayuda#usuarios", "/Account/CreateUser#Nombre")]
    public void Buscar_MuestraElTemaDeAyuda(string consulta, string urlAyuda, string? primeraPantalla)
    {
        var resultados = BuscadorCatalogo.Buscar(consulta, Principal(UserRoles.Administrador));

        Assert.Contains(resultados, a => a.Url == urlAyuda);
        if (primeraPantalla is not null)
            Assert.Equal(primeraPantalla, resultados[0].Url);
        Assert.InRange(resultados.Count, 1, BuscadorCatalogo.MaximoResultados);
    }

    [Fact]
    public void Instructor_NoRecibeAyudaDeUsuarios()
    {
        var resultados = BuscadorCatalogo.Buscar("crear usuario", Principal(UserRoles.Instructor));

        Assert.DoesNotContain(resultados, a => a.Url.Contains("usuarios", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resultados, a => a.Url.Contains("/Account/Users", StringComparison.Ordinal));
    }

    [Fact]
    public void RutaDeOrdenes_AbreElTemaDeOrdenes()
    {
        var tema = AyudaCatalogo.ParaRuta("Ordenes", "Index", Principal(UserRoles.Instructor));

        Assert.NotNull(tema);
        Assert.Equal("ordenes", tema!.Id);
    }

    private static HashSet<string> Ids(string rol) =>
        AyudaCatalogo.Visibles(Principal(rol)).Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

    private static ClaimsPrincipal Principal(string rol)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, rol),
            new Claim(ClaimTypes.Name, "Usuario")
        ], "Test");
        return new ClaimsPrincipal(identity);
    }
}
