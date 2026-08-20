using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Authorization;

namespace Sipitex.Tests;

public class CurrentBodegaAccessorTests
{
    [Fact]
    public void BodegaIds_SinHttpContext_EsNull()
    {
        var http = new Mock<IHttpContextAccessor>();
        http.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

        Assert.Null(new CurrentBodegaAccessor(http.Object).BodegaIds);
    }

    [Fact]
    public void BodegaIds_Administrador_EsNull()
    {
        var accessor = ForUser(UserRoles.Administrador, bodegaClaims: [1]);
        Assert.Null(accessor.BodegaIds);
    }

    [Fact]
    public void BodegaIds_Instructor_EsNull()
    {
        var accessor = ForUser(UserRoles.Instructor, bodegaClaims: [2]);
        Assert.Null(accessor.BodegaIds);
    }

    [Fact]
    public void BodegaIds_BodegueroConClaim_DevuelveBodega()
    {
        var accessor = ForUser(UserRoles.Bodeguero, bodegaClaims: [2]);
        Assert.Equal([2], accessor.BodegaIds);
    }

    [Fact]
    public void BodegaIds_BodegueroConVariosClaims_DevuelveTodas()
    {
        var accessor = ForUser(UserRoles.Bodeguero, bodegaClaims: [1, 2]);
        Assert.Equal([1, 2], accessor.BodegaIds);
    }

    [Fact]
    public void BodegaIds_BodegueroSinClaim_DevuelveListaVacia()
    {
        var accessor = ForUser(UserRoles.Bodeguero, bodegaClaims: []);
        Assert.NotNull(accessor.BodegaIds);
        Assert.Empty(accessor.BodegaIds!);
    }

    private static CurrentBodegaAccessor ForUser(string role, int[] bodegaClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "5"),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, "Test")
        };
        foreach (var id in bodegaClaims.Where(id => id > 0))
            claims.Add(new Claim(BodegaClaimTypes.BodegaId, id.ToString()));

        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"))
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(h => h.HttpContext).Returns(http);
        return new CurrentBodegaAccessor(accessor.Object);
    }
}
