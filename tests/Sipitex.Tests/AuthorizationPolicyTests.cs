using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Sipitex.Application.Authorization;
using Sipitex.Domain.Entities;
using Sipitex.Web.Controllers;

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
    public void PlantasInventarioSolicitudes_Resolve_UsaPuedeAprobarSolicitudes()
    {
        var attr = typeof(PlantasInventarioSolicitudesController)
            .GetMethod(nameof(PlantasInventarioSolicitudesController.Resolve))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AuthorizationPolicyNames.PuedeAprobarSolicitudes, attr!.Policy);
        Assert.True(string.IsNullOrEmpty(attr.Roles));
    }

    [Fact]
    public void PlantasInventario_IndexYConsultar_UsanPuedeConsultarInventario()
    {
        foreach (var name in new[]
                 {
                     nameof(PlantasInventarioController.Index),
                     nameof(PlantasInventarioController.Consultar)
                 })
        {
            var attr = typeof(PlantasInventarioController)
                .GetMethod(name)!
                .GetCustomAttributes<AuthorizeAttribute>()
                .First(a => !string.IsNullOrEmpty(a.Policy));
            Assert.Equal(AuthorizationPolicyNames.PuedeConsultarInventario, attr.Policy);
        }
    }
}
