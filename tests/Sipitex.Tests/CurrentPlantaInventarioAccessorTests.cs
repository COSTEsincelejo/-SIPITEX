using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Authorization;

namespace Sipitex.Tests;

public class CurrentPlantaInventarioAccessorTests
{
    [Fact]
    public void PlantaInventarioIds_SinHttpContext_EsNull()
    {
        var http = new Mock<IHttpContextAccessor>();
        http.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

        Assert.Null(new CurrentPlantaInventarioAccessor(http.Object).PlantaInventarioIds);
    }

    [Fact]
    public void PlantaInventarioIds_Administrador_EsNull()
    {
        var accessor = ForUser(UserRoles.Administrador, plantaClaims: [1]);
        Assert.Null(accessor.PlantaInventarioIds);
    }

    [Fact]
    public void PlantaInventarioIds_Instructor_EsNull()
    {
        var accessor = ForUser(UserRoles.Instructor, plantaClaims: [2]);
        Assert.Null(accessor.PlantaInventarioIds);
    }

    [Fact]
    public void PlantaInventarioIds_EncargadoDeBodegaConClaim_DevuelvePlantaInventario()
    {
        var accessor = ForUser(UserRoles.EncargadoDeBodega, plantaClaims: [2]);
        Assert.Equal([2], accessor.PlantaInventarioIds);
    }

    [Fact]
    public void PlantaInventarioIds_EncargadoDeBodegaConVariosClaims_DevuelveTodas()
    {
        var accessor = ForUser(UserRoles.EncargadoDeBodega, plantaClaims: [1, 2]);
        Assert.Equal([1, 2], accessor.PlantaInventarioIds);
    }

    [Fact]
    public void PlantaInventarioIds_EncargadoDeBodegaSinClaim_DevuelveListaVacia()
    {
        var accessor = ForUser(UserRoles.EncargadoDeBodega, plantaClaims: []);
        Assert.NotNull(accessor.PlantaInventarioIds);
        Assert.Empty(accessor.PlantaInventarioIds!);
    }

    private static CurrentPlantaInventarioAccessor ForUser(string role, int[] plantaClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "5"),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, "Test")
        };
        foreach (var id in plantaClaims.Where(id => id > 0))
            claims.Add(new Claim(PlantaInventarioClaimTypes.PlantaInventarioId, id.ToString()));

        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"))
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(h => h.HttpContext).Returns(http);
        return new CurrentPlantaInventarioAccessor(accessor.Object);
    }
}
