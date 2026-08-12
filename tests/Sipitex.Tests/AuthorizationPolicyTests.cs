using System.Security.Claims;
using Sipitex.Application.Authorization;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class AuthorizationPolicyTests
{
    private static ClaimsPrincipal CreateInstructor(params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "42"),
            new(ClaimTypes.Name, "Instructor Test"),
            new(ClaimTypes.Role, UserRoles.Instructor)
        };

        foreach (var permission in permissions)
            claims.Add(new Claim(ExtendedPermissions.ClaimType, permission));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    [Fact]
    public void PuedeRegistrarMateriales_InstructorWithoutClaim_IsDenied()
    {
        var user = CreateInstructor();
        Assert.False(PermissionRules.PuedeRegistrarMateriales(user));
    }

    [Fact]
    public void PuedeRegistrarMateriales_InstructorWithClaim_IsAllowed()
    {
        var user = CreateInstructor(ExtendedPermissions.InventarioRegistrar);
        Assert.True(PermissionRules.PuedeRegistrarMateriales(user));
    }

    [Fact]
    public void PuedeAprobarSolicitudes_InstructorWithoutClaim_IsDenied()
    {
        var user = CreateInstructor();
        Assert.False(PermissionRules.PuedeAprobarSolicitudes(user));
    }

    [Fact]
    public void PuedeAprobarSolicitudes_InstructorWithClaim_IsAllowed()
    {
        var user = CreateInstructor(ExtendedPermissions.SolicitudesAprobar);
        Assert.True(PermissionRules.PuedeAprobarSolicitudes(user));
    }

    [Fact]
    public void PuedeAccederInventario_InstructorWithoutClaim_IsDenied()
    {
        var user = CreateInstructor();
        Assert.False(PermissionRules.PuedeAccederInventario(user));
    }

    [Fact]
    public void PuedeAccederInventario_InstructorWithClaim_IsAllowed()
    {
        var user = CreateInstructor(ExtendedPermissions.InventarioRegistrar);
        Assert.True(PermissionRules.PuedeAccederInventario(user));
    }
}
