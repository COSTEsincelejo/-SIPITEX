using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

public class AuditoriaControllerTests
{
    [Fact]
    public void AuditoriaController_SoloAdministrador_NoBodegueroNiInstructor()
    {
        var classAttr = typeof(AuditoriaController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAttr);
        Assert.Equal(UserRoles.Administrador, classAttr!.Roles);
        Assert.DoesNotContain(UserRoles.Bodeguero, classAttr.Roles!, StringComparison.Ordinal);
        Assert.DoesNotContain(UserRoles.Instructor, classAttr.Roles!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Index_PasaFiltrosAlServicio()
    {
        var logs = new Mock<IActivityLogService>();
        logs.Setup(s => s.QueryAsync(
                It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        logs.Setup(s => s.GetDistinctActionsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(["CreateOrder"]);
        logs.Setup(s => s.GetDistinctEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(["ProductionOrder"]);
        logs.Setup(s => s.GetDistinctActorsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var controller = new AuditoriaController(logs.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var desde = new DateOnly(2026, 8, 1);
        var result = await controller.Index(desde, null, ActivityLogActions.CreateOrder, null, 7, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        logs.Verify(s => s.QueryAsync(
            desde, null, ActivityLogActions.CreateOrder, null, 7, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class OrdenesActivityLogInstrumentationTests
{
    private readonly Mock<IProductionOrderService> _orders = new();
    private readonly Mock<IBomCatalogService> _bom = new();
    private readonly Mock<IOrderMaterialService> _materials = new();
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<IProductionFlowService> _flow = new();
    private readonly Mock<IUserAccountService> _users = new();
    private readonly Mock<IActivityLogService> _activity = new();

    private OrdenesController CreateController()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Admin"),
            new Claim(ClaimTypes.Role, UserRoles.Administrador)
        ], "Test");

        var controller = new OrdenesController(
            _orders.Object,
            _bom.Object,
            _materials.Object,
            _inventory.Object,
            _flow.Object,
            _users.Object,
            _activity.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task Create_OnSuccess_LogsCreateOrder()
    {
        _orders.Setup(s => s.CreateOrderAsync(It.IsAny<CreateProductionOrderDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Ok("Orden OP-101 creada (pendiente de aprobación)."));

        var controller = CreateController();
        try
        {
            await controller.Create(new CreateOrderForm
            {
                ProductName = "Camisa",
                TotalQuantity = 20,
                Deadline = new DateOnly(2026, 9, 1)
            }, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // RedirectToAction sin pipeline MVC
        }

        _activity.Verify(a => a.LogAsync(
            1,
            ActivityLogActions.CreateOrder,
            ActivityLogEntities.ProductionOrder,
            "Camisa",
            It.Is<string?>(d => d != null && d.Contains("Cantidad=20", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_OnFailure_DoesNotLog()
    {
        _orders.Setup(s => s.CreateOrderAsync(It.IsAny<CreateProductionOrderDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Fail("Producto no válido."));

        var controller = CreateController();
        try
        {
            await controller.Create(new CreateOrderForm { ProductName = "X", TotalQuantity = 1 }, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }

        _activity.Verify(a => a.LogAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class MrpActivityLogInstrumentationTests
{
    private readonly Mock<IMrpService> _mrp = new();
    private readonly Mock<IBomCatalogService> _bom = new();
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<IActivityLogService> _activity = new();

    private MrpController CreateController()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Admin"),
            new Claim(ClaimTypes.Role, UserRoles.Administrador)
        ], "Test");

        var controller = new MrpController(_mrp.Object, _bom.Object, _inventory.Object, _activity.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    [Fact]
    public async Task Edit_OnSuccess_LogsUpdateBom()
    {
        _bom.Setup(s => s.UpdateAsync(5, It.IsAny<UpsertBomProductDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Ok("Ficha técnica «Camisa» actualizada."));

        var controller = CreateController();
        var form = new BomProductEditForm { ProductName = "Camisa" };
        try
        {
            await controller.Edit(5, form, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
        }

        _activity.Verify(a => a.LogAsync(
            1,
            ActivityLogActions.UpdateBom,
            ActivityLogEntities.BomProduct,
            "5",
            It.Is<string?>(d => d != null && d.Contains("Camisa", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Edit_OnFailure_DoesNotLog()
    {
        _bom.Setup(s => s.UpdateAsync(5, It.IsAny<UpsertBomProductDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Fail("Nombre duplicado."));
        _bom.Setup(s => s.GetProductAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProductDetailDto?)null);
        _inventory.Setup(s => s.GetMaterialsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var controller = CreateController();
        await controller.Edit(5, new BomProductEditForm { ProductName = "Camisa" }, CancellationToken.None);

        _activity.Verify(a => a.LogAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
