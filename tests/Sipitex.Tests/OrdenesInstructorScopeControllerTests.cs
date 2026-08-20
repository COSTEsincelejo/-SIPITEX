using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

/// <summary>
/// Gap #8: OrdenesController cablea GetOrdersAsync / CanAccessOrderAsync (sin cambiar el servicio).
/// </summary>
public class OrdenesInstructorScopeControllerTests
{
    private readonly Mock<IProductionOrderService> _orders = new();
    private readonly Mock<IBomCatalogService> _bom = new();
    private readonly Mock<IOrderMaterialService> _materials = new();
    private readonly Mock<IInventoryService> _inventory = new();
    private readonly Mock<IProductionFlowService> _flow = new();
    private readonly Mock<IUserAccountService> _users = new();

    private OrdenesController CreateController(ClaimsPrincipal user)
    {
        var controller = new OrdenesController(
            _orders.Object,
            _bom.Object,
            _materials.Object,
            _inventory.Object,
            _flow.Object,
            _users.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            Mock.Of<ITempDataProvider>());
        return controller;
    }

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

    private static ProductionOrderDto OrderDto(int id, string number) => new(
        id,
        number,
        "Camisa",
        10,
        0,
        0,
        OrderStatus.EnProceso,
        DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
        "",
        OrderMaterialsStatus.NoAplica,
        false,
        true);

    [Fact]
    public async Task Index_Instructor_PassesViewerToGetOrdersAsync()
    {
        _orders.Setup(s => s.GetOrdersAsync(10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync([OrderDto(1, "OP-101")]);
        _bom.Setup(b => b.GetOrderEligibleProductNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Camisa"]);

        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));
        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<OrdenesIndexViewModel>(view.Model);
        Assert.Single(vm.Orders);
        Assert.Equal("OP-101", vm.Orders[0].OrderNumber);

        _orders.Verify(s => s.GetOrdersAsync(
            10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()), Times.Once);
        _orders.Verify(s => s.GetOrdersAsync(
            null, null, null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Index_Administrador_PassesViewer_AndServiceReturnsAll()
    {
        _orders.Setup(s => s.GetOrdersAsync(1, UserRoles.Administrador, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync([OrderDto(1, "OP-101"), OrderDto(2, "OP-102")]);
        _bom.Setup(b => b.GetOrderEligibleProductNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Camisa"]);

        var controller = CreateController(Principal(1, UserRoles.Administrador, "Admin"));
        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<OrdenesIndexViewModel>(view.Model);
        Assert.Equal(2, vm.Orders.Count);
    }

    [Fact]
    public async Task Detail_Instructor_ForeignOrder_ReturnsForbid()
    {
        _orders.Setup(s => s.CanAccessOrderAsync(99, 10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _orders.Setup(s => s.AuthorizeOrderMaterialsAsync(99, 10, UserRoles.Instructor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Fail("No autorizado"));

        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));
        var result = await controller.Detail(99, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        _flow.Verify(f => f.GetMesDetailAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddProduction_Instructor_ForeignOrder_ReturnsForbid()
    {
        _orders.Setup(s => s.CanAccessOrderAsync(99, 10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));
        var result = await controller.AddProduction(99, units: 5, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        _orders.Verify(s => s.RegisterProductionAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddMaterial_Instructor_WithoutMaterialsGate_RedirectsWithoutCallingService()
    {
        _orders.Setup(s => s.AuthorizeOrderMaterialsAsync(99, 10, UserRoles.Instructor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Fail("No está habilitado en la ficha técnica ni asignado a una etapa de esta orden."));

        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));
        var result = await controller.AddMaterial(
            new AddOrderMaterialForm { OrderId = 99, MaterialId = 1, QuantityRequired = 2 },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(OrdenesController.Detail), redirect.ActionName);
        Assert.False((bool)controller.TempData["IsSuccess"]!);
        _materials.Verify(m => m.AddMaterialAsync(
            It.IsAny<AddOrderMaterialDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Detail_Instructor_AssignedOrder_Continues()
    {
        _orders.Setup(s => s.CanAccessOrderAsync(1, 10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _orders.Setup(s => s.AuthorizeOrderMaterialsAsync(1, 10, UserRoles.Instructor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Ok());
        _flow.Setup(f => f.GetMesDetailAsync(1, 10, UserRoles.Instructor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderMesDetailDto(
                1, "OP-101", "Camisa", null, OrderStatus.EnProceso, OrderMaterialsStatus.NoAplica,
                10, 0, 0, 0, 0, null, DateOnly.FromDateTime(DateTime.Today.AddDays(14)), "", 0, false,
                [], [], [], [], []));
        _inventory.Setup(i => i.GetMaterialsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _users.Setup(u => u.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _orders.Setup(s => s.GetChangeLogAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));
        var result = await controller.Detail(1, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
    }
}
