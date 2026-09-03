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
    public void BodegaIds_SinHttpContext_EsNull()
    {
        var http = new Mock<IHttpContextAccessor>();
        http.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

        Assert.Null(new CurrentPlantaInventarioAccessor(http.Object).PlantaInventarioIds);
    }

    [Fact]
    public void BodegaIds_Administrador_EsNull()
    {
        var accessor = ForUser(UserRoles.Administrador, plantaInventarioClaims: [1]);
        Assert.Null(accessor.PlantaInventarioIds);
    }

    [Fact]
    public void BodegaIds_Instructor_EsNull()
    {
        var accessor = ForUser(UserRoles.Instructor, plantaInventarioClaims: [2]);
        Assert.Null(accessor.PlantaInventarioIds);
    }

    [Fact]
    public void BodegaIds_BodegueroConClaim_DevuelveBodega()
    {
        var accessor = ForUser(UserRoles.EncargadoBodega, plantaInventarioClaims: [2]);
        Assert.Equal([2], accessor.PlantaInventarioIds);
    }

    [Fact]
    public void BodegaIds_BodegueroConVariosClaims_DevuelveTodas()
    {
        var accessor = ForUser(UserRoles.EncargadoBodega, plantaInventarioClaims: [1, 2]);
        Assert.Equal([1, 2], accessor.PlantaInventarioIds);
    }

    [Fact]
    public void BodegaIds_BodegueroSinClaim_DevuelveListaVacia()
    {
        var accessor = ForUser(UserRoles.EncargadoBodega, plantaInventarioClaims: []);
        Assert.NotNull(accessor.PlantaInventarioIds);
        Assert.Empty(accessor.PlantaInventarioIds!);
    }

    private static CurrentPlantaInventarioAccessor ForUser(string role, int[] plantaInventarioClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "5"),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, "Test")
        };
        foreach (var id in plantaInventarioClaims.Where(id => id > 0))
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
