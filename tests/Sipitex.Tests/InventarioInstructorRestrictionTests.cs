using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
/// Gap #12: Instructor sin acceso al Inventario general; MaterialRequest legacy filtrado por SolicitanteId.
/// </summary>
public class InventarioInstructorRestrictionTests
{
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IMaterialRequestRepository> _requests = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<IBodegaRepository> _bodegas = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private InventoryService CreateInventoryService() => new(
        _materials.Object,
        _requests.Object,
        _orders.Object,
        _boms.Object,
        _stockMovements.Object,
        _bodegas.Object,
        _uow.Object);

    private static ClaimsPrincipal Principal(int userId, string role, string name = "Usuario")
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, name)
        ], "Test");
        return new ClaimsPrincipal(identity);
    }

    private static InventarioController CreateController(
        ClaimsPrincipal user,
        IInventoryService inventory,
        IProductionOrderService orders,
        IStockMovementService movements)
    {
        var bodegas = new Mock<IBodegaRepository>();
        bodegas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = new InventarioController(
            inventory,
            orders,
            movements,
            Mock.Of<IUserAccountService>(),
            bodegas.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            controller.HttpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return controller;
    }

    private static MaterialRequest Request(
        int id,
        int? solicitanteId,
        string materialName = "Tela",
        string orderNumber = "OP-101") =>
        new()
        {
            Id = id,
            MaterialId = 1,
            Material = new Material { Id = 1, Name = materialName, Stock = 10, Unit = MaterialUnit.Metros },
            ProductionOrderId = 1,
            ProductionOrder = new ProductionOrder { Id = 1, OrderNumber = orderNumber, ProductName = "Camisa" },
            Quantity = 2,
            Status = RequestStatus.Pendiente,
            SolicitanteId = solicitanteId
        };

    [Fact]
    public void PuedeConsultarInventario_Instructor_IsDenied()
    {
        Assert.False(PermissionRules.PuedeConsultarInventario(Principal(10, UserRoles.Instructor)));
        Assert.False(PermissionRules.PuedeConsultarInventario(
            Principal(10, UserRoles.Instructor))); // sin permisos extendidos
    }

    [Fact]
    public void PuedeConsultarInventario_InstructorWithInventarioRegistrar_StillDenied()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "10"),
            new(ClaimTypes.Role, UserRoles.Instructor),
            new(ClaimTypes.Name, "Laura"),
            new(ExtendedPermissions.ClaimType, ExtendedPermissions.InventarioRegistrar)
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        Assert.False(PermissionRules.PuedeConsultarInventario(user));
    }

    [Fact]
    public void PuedeConsultarInventario_AdminAndBodeguero_Allowed()
    {
        Assert.True(PermissionRules.PuedeConsultarInventario(Principal(1, UserRoles.Administrador)));
        Assert.True(PermissionRules.PuedeConsultarInventario(Principal(2, UserRoles.Bodeguero)));
    }

    [Fact]
    public void InventarioController_Index_RequiresPuedeConsultarInventarioPolicy()
    {
        var method = typeof(InventarioController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nameof(InventarioController.Index) && m.GetParameters().Length == 2);
        var attr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AuthorizationPolicyNames.PuedeConsultarInventario, attr!.Policy);
    }

    [Fact]
    public async Task InventarioController_Index_Instructor_ReturnsForbid()
    {
        var inventory = new Mock<IInventoryService>();
        var orders = new Mock<IProductionOrderService>();
        var movements = new Mock<IStockMovementService>();
        var controller = CreateController(
            Principal(10, UserRoles.Instructor, "Laura"),
            inventory.Object,
            orders.Object,
            movements.Object);

        var result = await controller.Index(bodegaId: null, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        inventory.Verify(
            s => s.GetMaterialsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        inventory.Verify(
            s => s.GetRequestsAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InventarioController_Index_Administrador_ReturnsView()
    {
        var inventory = new Mock<IInventoryService>();
        inventory.Setup(s => s.GetMaterialsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        inventory.Setup(s => s.GetRequestsAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var orders = new Mock<IProductionOrderService>();
        orders.Setup(s => s.GetOrdersAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var movements = new Mock<IStockMovementService>();
        var controller = CreateController(
            Principal(1, UserRoles.Administrador, "Admin"),
            inventory.Object,
            orders.Object,
            movements.Object);

        var result = await controller.Index(bodegaId: null, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task GetRequestsAsync_Instructor_SeesOnlyOwnSolicitanteRequests()
    {
        _requests.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Request(1, solicitanteId: 10, orderNumber: "OP-OWN"),
                Request(2, solicitanteId: 99, orderNumber: "OP-OTHER"),
                Request(3, solicitanteId: null, orderNumber: "OP-LEGACY")
            ]);

        var list = await CreateInventoryService().GetRequestsAsync(
            viewerUserId: 10,
            viewerRole: UserRoles.Instructor);

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("OP-OWN", list[0].OrderNumber);
    }

    [Fact]
    public async Task GetRequestsAsync_Administrador_SeesAllRequests()
    {
        _requests.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Request(1, solicitanteId: 10),
                Request(2, solicitanteId: 99),
                Request(3, solicitanteId: null)
            ]);

        var list = await CreateInventoryService().GetRequestsAsync(
            viewerUserId: 1,
            viewerRole: UserRoles.Administrador);

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task GetRequestsAsync_Bodeguero_SeesAllRequests()
    {
        _requests.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Request(1, solicitanteId: 10),
                Request(2, solicitanteId: 99)
            ]);

        var list = await CreateInventoryService().GetRequestsAsync(
            viewerUserId: 5,
            viewerRole: UserRoles.Bodeguero);

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task CreateRequestAsync_PersistsSolicitanteId()
    {
        _materials.Setup(m => m.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 1, Name = "Tela", Stock = 5, Unit = MaterialUnit.Metros });
        _orders.Setup(o => o.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 7, OrderNumber = "OP-107", ProductName = "Pantalón" });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        MaterialRequest? saved = null;
        _requests
            .Setup(r => r.AddAsync(It.IsAny<MaterialRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MaterialRequest, CancellationToken>((r, _) => saved = r)
            .Returns(Task.CompletedTask);

        var result = await CreateInventoryService().CreateRequestAsync(
            new CreateMaterialRequestDto(7, 1, 3),
            solicitanteId: 10);

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal(10, saved!.SolicitanteId);
    }
}
