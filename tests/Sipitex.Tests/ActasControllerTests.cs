using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Sipitex.Domain.Entities;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

public class ActasControllerTests
{
    [Fact]
    public void ActasController_AdminInstructorYEncargado()
    {
        var classAttr = typeof(ActasController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAttr);
        Assert.Contains(UserRoles.Administrador, classAttr!.Roles!, StringComparison.Ordinal);
        Assert.Contains(UserRoles.Instructor, classAttr.Roles!, StringComparison.Ordinal);
        Assert.Contains(UserRoles.EncargadoDeBodega, classAttr.Roles!, StringComparison.Ordinal);
    }
}
