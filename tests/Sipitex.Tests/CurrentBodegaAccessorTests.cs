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
    public void BodegaId_SinHttpContext_EsNull()
    {
        var http = new Mock<IHttpContextAccessor>();
        http.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

        Assert.Null(new CurrentBodegaAccessor(http.Object).BodegaId);
    }

    [Fact]
    public void BodegaId_Administrador_EsNull()
    {
        var accessor = ForUser(UserRoles.Administrador, bodegaClaim: 1);
        Assert.Null(accessor.BodegaId);
    }

    [Fact]
    public void BodegaId_Instructor_EsNull()
    {
        var accessor = ForUser(UserRoles.Instructor, bodegaClaim: 2);
        Assert.Null(accessor.BodegaId);
    }

    [Fact]
    public void BodegaId_BodegueroConClaim_DevuelveBodega()
    {
        var accessor = ForUser(UserRoles.Bodeguero, bodegaClaim: 2);
        Assert.Equal(2, accessor.BodegaId);
    }

    [Fact]
    public void BodegaId_BodegueroSinClaim_DevuelveCero()
    {
        var accessor = ForUser(UserRoles.Bodeguero, bodegaClaim: null);
        Assert.Equal(0, accessor.BodegaId);
    }

    private static CurrentBodegaAccessor ForUser(string role, int? bodegaClaim)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "5"),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, "Test")
        };
        if (bodegaClaim is > 0)
            claims.Add(new Claim(BodegaClaimTypes.BodegaId, bodegaClaim.Value.ToString()));

        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"))
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(h => h.HttpContext).Returns(http);
        return new CurrentBodegaAccessor(accessor.Object);
    }
}
