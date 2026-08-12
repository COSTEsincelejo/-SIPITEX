using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

/// <summary>
/// Gap #4 (AUDITORIA_ROLES_FUNCIONES): asignación M2M de ficha técnica (BomProduct) a instructores.
/// </summary>
public class BomProductInstructorAssignmentTests
{
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private BomCatalogService CreateSut() =>
        new(_boms.Object, _materials.Object, _users.Object, _orders.Object, _uow.Object);

    private static User Instructor(int id, string name, bool active = true) => new()
    {
        Id = id,
        Nombre = name,
        Email = $"{name.Replace(" ", "").ToLowerInvariant()}@test.local",
        Rol = UserRoles.Instructor,
        IsActive = active,
        PasswordHash = "x"
    };

    private static BomProduct Product(int id, string name, params BomProductInstructor[] instructors) => new()
    {
        Id = id,
        ProductName = name,
        Items = [],
        Instructors = instructors.ToList()
    };

    [Fact]
    public async Task AssignInstructorAsync_AddsInstructorToBomProduct()
    {
        var product = Product(5, "Camisa");
        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _users.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Instructor(20, "Carlos Méndez"));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().AssignInstructorAsync(5, 20);

        Assert.True(result.Success);
        Assert.Single(product.Instructors);
        Assert.Equal(20, product.Instructors.First().UserId);
        _boms.Verify(r => r.UpdateProduct(product), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignInstructorAsync_WhenAlreadyAssigned_Fails()
    {
        var product = Product(5, "Camisa",
            new BomProductInstructor { BomProductId = 5, UserId = 20, User = Instructor(20, "Carlos") });
        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _users.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Instructor(20, "Carlos"));

        var result = await CreateSut().AssignInstructorAsync(5, 20);

        Assert.False(result.Success);
        Assert.Contains("ya está asignado", result.Message);
    }

    [Fact]
    public async Task AssignInstructorAsync_WhenUserIsNotInstructor_Fails()
    {
        var product = Product(5, "Camisa");
        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _users.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = 1,
            Nombre = "Admin",
            Rol = UserRoles.Administrador,
            IsActive = true,
            PasswordHash = "x"
        });

        var result = await CreateSut().AssignInstructorAsync(5, 1);

        Assert.False(result.Success);
        Assert.Contains("rol Instructor", result.Message);
        Assert.Empty(product.Instructors);
    }

    [Fact]
    public async Task RemoveInstructorAsync_UnassignsInstructor()
    {
        var product = Product(5, "Camisa",
            new BomProductInstructor { BomProductId = 5, UserId = 10, User = Instructor(10, "Laura") },
            new BomProductInstructor { BomProductId = 5, UserId = 20, User = Instructor(20, "Carlos") });
        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().RemoveInstructorAsync(5, 10);

        Assert.True(result.Success);
        Assert.Single(product.Instructors);
        Assert.DoesNotContain(product.Instructors, i => i.UserId == 10);
    }

    [Fact]
    public async Task GetProductsAsync_Instructor_SeesOnlyAssignedBomProducts()
    {
        _boms.Setup(r => r.GetProductsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            Product(1, "Camisa",
                new BomProductInstructor { BomProductId = 1, UserId = 10, User = Instructor(10, "Laura") }),
            Product(2, "Pantalón",
                new BomProductInstructor { BomProductId = 2, UserId = 20, User = Instructor(20, "Carlos") }),
            Product(3, "Overol",
                new BomProductInstructor { BomProductId = 3, UserId = 10, User = Instructor(10, "Laura") },
                new BomProductInstructor { BomProductId = 3, UserId = 99, User = Instructor(99, "Otro") })
        ]);

        var forLaura = await CreateSut().GetProductsAsync(assignedInstructorUserId: 10);
        var all = await CreateSut().GetProductsAsync();

        Assert.Equal(2, forLaura.Count);
        Assert.Contains(forLaura, p => p.ProductName == "Camisa");
        Assert.Contains(forLaura, p => p.ProductName == "Overol");
        Assert.DoesNotContain(forLaura, p => p.ProductName == "Pantalón");
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void MrpController_AssignAndRemoveInstructor_AreAdministradorOnly()
    {
        foreach (var methodName in new[]
                 {
                     nameof(MrpController.AssignInstructor),
                     nameof(MrpController.RemoveInstructor)
                 })
        {
            var method = typeof(MrpController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Single(m => m.Name == methodName);
            var attr = method.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attr);
            Assert.Equal(UserRoles.Administrador, attr!.Roles);
            Assert.DoesNotContain(UserRoles.Instructor, attr.Roles!, StringComparison.Ordinal);
            Assert.DoesNotContain(UserRoles.Bodeguero, attr.Roles!, StringComparison.Ordinal);
            Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        }
    }
}
