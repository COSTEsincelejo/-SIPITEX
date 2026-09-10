using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Sipitex.Domain.Entities;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

public class CostosControllerTests
{
    [Fact]
    public void CostosController_AdminEInstructor_NoEncargadoDeBodega()
    {
        var classAttr = typeof(CostosController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAttr);
        Assert.Contains(UserRoles.Administrador, classAttr!.Roles!, StringComparison.Ordinal);
        Assert.Contains(UserRoles.Instructor, classAttr.Roles!, StringComparison.Ordinal);
        Assert.DoesNotContain(UserRoles.EncargadoDeBodega, classAttr.Roles!, StringComparison.Ordinal);
    }
}
