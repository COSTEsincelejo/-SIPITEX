using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sipitex.Application.Authorization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

/// <summary>
/// Gap #6 (AUDITORIA_ROLES_FUNCIONES): Instructor gestiona fichas técnicas solo con Mrp.GestionarFichas.
/// </summary>
public class MrpGestionarFichasPermissionTests
{
    private static ClaimsPrincipal CreatePrincipal(string role, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "42"),
            new(ClaimTypes.Name, "Usuario Test"),
            new(ClaimTypes.Role, role)
        };
        foreach (var permission in permissions)
            claims.Add(new Claim(ExtendedPermissions.ClaimType, permission));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    [Fact]
    public void PuedeGestionarFichasTecnicas_InstructorWithoutClaim_IsDenied()
    {
        var user = CreatePrincipal(UserRoles.Instructor);
        Assert.False(PermissionRules.PuedeGestionarFichasTecnicas(user));
    }

    [Fact]
    public void PuedeGestionarFichasTecnicas_InstructorWithClaim_IsAllowed()
    {
        var user = CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.MrpGestionarFichas);
        Assert.True(PermissionRules.PuedeGestionarFichasTecnicas(user));
    }

    [Fact]
    public void PuedeGestionarFichasTecnicas_Bodeguero_IsAllowedWithoutClaim()
    {
        var user = CreatePrincipal(UserRoles.Bodeguero);
        Assert.True(PermissionRules.PuedeGestionarFichasTecnicas(user));
    }

    [Fact]
    public void PuedeGestionarFichasTecnicas_Administrador_IsAllowedWithoutClaim()
    {
        var user = CreatePrincipal(UserRoles.Administrador);
        Assert.True(PermissionRules.PuedeGestionarFichasTecnicas(user));
    }

    [Fact]
    public void ExtendedPermissions_Catalog_IncludesMrpGestionarFichas()
    {
        Assert.Contains(ExtendedPermissions.MrpGestionarFichas, ExtendedPermissions.All);
        Assert.Contains(ExtendedPermissions.Catalog, c => c.Key == ExtendedPermissions.MrpGestionarFichas);
        Assert.Equal(
            ExtendedPermissions.MrpGestionarFichas,
            ExtendedPermissions.Parse(ExtendedPermissions.MrpGestionarFichas).Single());
    }

    [Fact]
    public void MrpController_CreateAndEdit_RequireGestionarFichasPolicy()
    {
        foreach (var methodName in new[] { nameof(MrpController.Create), nameof(MrpController.Edit) })
        {
            var methods = typeof(MrpController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.Name == methodName);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<AuthorizeAttribute>();
                Assert.NotNull(attr);
                Assert.Equal(AuthorizationPolicyNames.PuedeGestionarFichasTecnicas, attr!.Policy);
                Assert.True(string.IsNullOrEmpty(attr.Roles));
            }
        }
    }

    [Fact]
    public void MrpController_Delete_RequiresGestionarFichasPolicy()
    {
        var method = typeof(MrpController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nameof(MrpController.Delete));
        var attr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AuthorizationPolicyNames.PuedeGestionarFichasTecnicas, attr!.Policy);
        Assert.True(string.IsNullOrEmpty(attr.Roles));
    }

    [Fact]
    public void InstructorWithGestionarFichas_MayDeleteByPolicy()
    {
        Assert.True(PermissionRules.PuedeGestionarFichasTecnicas(
            CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.MrpGestionarFichas)));
    }

    [Fact]
    public async Task InstructorWithPermission_CanDeleteBomProductNotInUse()
    {
        Assert.True(PermissionRules.PuedeGestionarFichasTecnicas(
            CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.MrpGestionarFichas)));

        var boms = new Mock<IBomRepository>();
        var materials = new Mock<IMaterialRepository>();
        var users = new Mock<IUserRepository>();
        var orders = new Mock<IProductionOrderRepository>();
        var uow = new Mock<IUnitOfWork>();

        var product = new BomProduct { Id = 9, ProductName = "Camiseta", Items = [] };
        boms.Setup(r => r.GetProductByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new BomCatalogService(boms.Object, materials.Object, users.Object, orders.Object, uow.Object);
        var result = await sut.DeleteAsync(9, CancellationToken.None);

        Assert.True(result.Success);
        boms.Verify(r => r.RemoveProduct(product), Times.Once);
    }

    [Fact]
    public async Task InstructorWithPermission_CanCreateAndUpdateBomProductViaCatalog()
    {
        // El gate de Forbid es la policy; con permiso el servicio de catálogo ejecuta Create/Update.
        Assert.True(PermissionRules.PuedeGestionarFichasTecnicas(
            CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.MrpGestionarFichas)));

        var boms = new Mock<IBomRepository>();
        var materials = new Mock<IMaterialRepository>();
        var users = new Mock<IUserRepository>();
        var orders = new Mock<IProductionOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material
            {
                Id = 1,
                Code = "mat1",
                Name = "Jersey",
                Unit = MaterialUnit.Metros,
                Stock = 10
            });
        boms.Setup(r => r.GetProductByNameAsync("Camiseta", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProduct?)null);
        boms.Setup(r => r.AddProductAsync(It.IsAny<BomProduct>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new BomCatalogService(boms.Object, materials.Object, users.Object, orders.Object, uow.Object);
        var create = await sut.CreateAsync(new UpsertBomProductDto(
            "Camiseta",
            IsReference: false,
            Notes: null,
            HabilitadoParaOrdenes: true,
            Lines: [new BomRecipeLineDto(null, 1, null, null, 0.8m, MaterialUnit.Metros)]));

        Assert.True(create.Success);

        var product = new BomProduct
        {
            Id = 7,
            ProductName = "Camiseta",
            Items =
            [
                new BomItem
                {
                    Id = 1,
                    BomProductId = 7,
                    MaterialId = 1,
                    Material = new Material { Id = 1, Name = "Jersey", Unit = MaterialUnit.Metros },
                    QuantityPerUnit = 0.8m,
                    Unit = MaterialUnit.Metros
                }
            ]
        };
        boms.Setup(r => r.GetProductByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        boms.Setup(r => r.GetProductByNameAsync("Camiseta v2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProduct?)null);

        var update = await sut.UpdateAsync(7, new UpsertBomProductDto(
            "Camiseta v2",
            IsReference: false,
            Notes: "ok",
            HabilitadoParaOrdenes: true,
            Lines: [new BomRecipeLineDto(1, 1, null, null, 1.0m, MaterialUnit.Metros)]));

        Assert.True(update.Success);
        Assert.Equal("Camiseta v2", product.ProductName);
    }

    [Fact]
    public void InstructorWithoutPermission_IsForbiddenByPolicyGate()
    {
        // Equivalente a Forbid de [Authorize(Policy = PuedeGestionarFichasTecnicas)]
        // en Create/Edit/Delete (misma policy).
        var user = CreatePrincipal(UserRoles.Instructor);
        Assert.False(PermissionRules.PuedeGestionarFichasTecnicas(user));
        Assert.False(PermissionRules.PuedeGestionarFichasTecnicas(
            CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.MrpSimular)));

        var deleteAttr = typeof(MrpController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nameof(MrpController.Delete))
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal(AuthorizationPolicyNames.PuedeGestionarFichasTecnicas, deleteAttr!.Policy);
    }

    [Fact]
    public async Task DeleteBlockedWhenActiveOrder_RegardlessOfRole_IncludingAdminAndInstructorWithPermission()
    {
        // El bloqueo es de dominio (BomCatalogService), no de rol: Admin e Instructor
        // con Mrp.GestionarFichas reciben el mismo Fail.
        Assert.True(PermissionRules.PuedeGestionarFichasTecnicas(
            CreatePrincipal(UserRoles.Administrador)));
        Assert.True(PermissionRules.PuedeGestionarFichasTecnicas(
            CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.MrpGestionarFichas)));

        var boms = new Mock<IBomRepository>();
        var materials = new Mock<IMaterialRepository>();
        var users = new Mock<IUserRepository>();
        var orders = new Mock<IProductionOrderRepository>();
        var uow = new Mock<IUnitOfWork>();

        var product = new BomProduct { Id = 9, ProductName = "Camiseta", Items = [] };
        boms.Setup(r => r.GetProductByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ProductionOrder
            {
                Id = 1,
                OrderNumber = "OP-77",
                ProductName = "Camiseta",
                Status = OrderStatus.EnProceso
            }
        ]);

        var sut = new BomCatalogService(boms.Object, materials.Object, users.Object, orders.Object, uow.Object);
        var result = await sut.DeleteAsync(9, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("OP-77", result.Message, StringComparison.Ordinal);
        boms.Verify(r => r.RemoveProduct(It.IsAny<BomProduct>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
