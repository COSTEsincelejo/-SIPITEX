using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sipitex.Application.Authorization;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

/// <summary>
/// Gap #7 (AUDITORIA_ROLES_FUNCIONES): Instructor crea órdenes con Ordenes.Crear y queda asignado.
/// </summary>
public class OrdenesCrearPermissionTests
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
    public void PuedeCrearOrdenes_InstructorWithoutClaim_IsDenied()
    {
        Assert.False(PermissionRules.PuedeCrearOrdenes(CreatePrincipal(UserRoles.Instructor)));
    }

    [Fact]
    public void PuedeCrearOrdenes_InstructorWithClaim_IsAllowed()
    {
        Assert.True(PermissionRules.PuedeCrearOrdenes(
            CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.OrdenesCrear)));
    }

    [Fact]
    public void PuedeCrearOrdenes_Administrador_IsAllowedWithoutClaim()
    {
        Assert.True(PermissionRules.PuedeCrearOrdenes(CreatePrincipal(UserRoles.Administrador)));
    }

    [Fact]
    public void PuedeCrearOrdenes_Bodeguero_IsDenied()
    {
        Assert.False(PermissionRules.PuedeCrearOrdenes(CreatePrincipal(UserRoles.Bodeguero)));
    }

    [Fact]
    public void ExtendedPermissions_Catalog_IncludesOrdenesCrear()
    {
        Assert.Contains(ExtendedPermissions.OrdenesCrear, ExtendedPermissions.All);
        Assert.Contains(ExtendedPermissions.Catalog, c => c.Key == ExtendedPermissions.OrdenesCrear);
    }

    [Fact]
    public void OrdenesController_Create_RequiresPuedeCrearOrdenesPolicy()
    {
        var method = typeof(OrdenesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nameof(OrdenesController.Create));
        var attr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AuthorizationPolicyNames.PuedeCrearOrdenes, attr!.Policy);
        Assert.True(string.IsNullOrEmpty(attr.Roles));
    }

    [Fact]
    public void InstructorWithoutPermission_IsForbiddenByPolicyGate()
    {
        Assert.False(PermissionRules.PuedeCrearOrdenes(CreatePrincipal(UserRoles.Instructor)));
        Assert.False(PermissionRules.PuedeCrearOrdenes(
            CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.MrpSimular)));
    }

    [Fact]
    public async Task CreateOrderAsync_WithResponsibleInstructor_AssignsAllMesStages()
    {
        var orders = new Mock<IProductionOrderRepository>();
        var boms = new Mock<IBomRepository>();
        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        var requirements = new Mock<IOrderMaterialRequirementRepository>();
        var flowRepo = new Mock<IProductionFlowRepository>();
        var flowService = new Mock<IProductionFlowService>();
        var changeLogs = new Mock<IOrderChangeLogRepository>();
        var uow = new Mock<IUnitOfWork>();
        var materials = new Mock<IMaterialRepository>();

        boms.Setup(r => r.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BomProduct
            {
                ProductName = "Camisa",
                HabilitadoParaOrdenes = true,
                Items =
                [
                    new BomItem
                    {
                        ProductName = "Camisa",
                        MaterialId = 1,
                        Material = new Material { Id = 1, Code = "mat1", Name = "Tela", Unit = MaterialUnit.Metros },
                        QuantityPerUnit = 1.6m,
                        Unit = MaterialUnit.Metros
                    }
                ]
            });
        orders.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        orders.Setup(r => r.AddAsync(It.IsAny<ProductionOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ProductionOrder, CancellationToken>((o, _) => o.Id = 9)
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        flowService.Setup(s => s.EnsureStagesForOrderAsync(9, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var stages = new List<ProductionOrderStage>
        {
            new() { Id = 1, ProductionOrderId = 9, Name = "Trazo", SortOrder = 1 },
            new() { Id = 2, ProductionOrderId = 9, Name = "Corte", SortOrder = 2 }
        };
        flowRepo.Setup(r => r.GetStagesByOrderAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(stages);

        var sut = new ProductionOrderService(
            orders.Object, boms.Object, snapshots.Object, requirements.Object,
            flowRepo.Object, flowService.Object, changeLogs.Object, uow.Object,
            new ProductionConsumptionService(boms.Object, materials.Object));

        Assert.True(PermissionRules.PuedeCrearOrdenes(
            CreatePrincipal(UserRoles.Instructor, ExtendedPermissions.OrdenesCrear)));

        var result = await sut.CreateOrderAsync(new CreateProductionOrderDto(
            "Camisa",
            50,
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            ClientName: null,
            ResponsibleInstructorUserId: 42));

        Assert.True(result.Success);
        Assert.All(stages, s => Assert.Equal(42, s.InstructorUserId));
        flowRepo.Verify(r => r.UpdateStage(It.IsAny<ProductionOrderStage>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateOrderAsync_WithoutResponsibleInstructor_DoesNotAssignStages()
    {
        var orders = new Mock<IProductionOrderRepository>();
        var boms = new Mock<IBomRepository>();
        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        var requirements = new Mock<IOrderMaterialRequirementRepository>();
        var flowRepo = new Mock<IProductionFlowRepository>();
        var flowService = new Mock<IProductionFlowService>();
        var changeLogs = new Mock<IOrderChangeLogRepository>();
        var uow = new Mock<IUnitOfWork>();
        var materials = new Mock<IMaterialRepository>();

        boms.Setup(r => r.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BomProduct
            {
                ProductName = "Camisa",
                HabilitadoParaOrdenes = true,
                Items =
                [
                    new BomItem
                    {
                        ProductName = "Camisa",
                        MaterialId = 1,
                        Material = new Material { Id = 1, Code = "mat1", Name = "Tela", Unit = MaterialUnit.Metros },
                        QuantityPerUnit = 1.6m,
                        Unit = MaterialUnit.Metros
                    }
                ]
            });
        orders.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        orders.Setup(r => r.AddAsync(It.IsAny<ProductionOrder>(), It.IsAny<CancellationToken>()))
            .Callback<ProductionOrder, CancellationToken>((o, _) => o.Id = 1)
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        flowService.Setup(s => s.EnsureStagesForOrderAsync(1, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new ProductionOrderService(
            orders.Object, boms.Object, snapshots.Object, requirements.Object,
            flowRepo.Object, flowService.Object, changeLogs.Object, uow.Object,
            new ProductionConsumptionService(boms.Object, materials.Object));

        var result = await sut.CreateOrderAsync(
            new CreateProductionOrderDto("Camisa", 10, DateOnly.FromDateTime(DateTime.Today)));

        Assert.True(result.Success);
        flowRepo.Verify(r => r.GetStagesByOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        flowRepo.Verify(r => r.UpdateStage(It.IsAny<ProductionOrderStage>()), Times.Never);
    }
}
