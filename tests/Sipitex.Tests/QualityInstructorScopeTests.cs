using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

/// <summary>
/// Gap #10 (AUDITORIA_ROLES_FUNCIONES): Instructor solo ve/registra calidad de órdenes asignadas.
/// </summary>
public class QualityInstructorScopeTests
{
    private readonly Mock<IQualityRepository> _quality = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionOrderService> _orderService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private QualityService CreateSut() => new(
        _quality.Object,
        _orders.Object,
        _orderService.Object,
        _uow.Object);

    private static QualityRecord Record(int orderId, string orderNumber, int units = 5) => new()
    {
        Id = orderId * 10,
        ProductionOrderId = orderId,
        ProductionOrder = new ProductionOrder
        {
            Id = orderId,
            OrderNumber = orderNumber,
            ProductName = "Camisa",
            Status = OrderStatus.EnProceso
        },
        UnitsInspected = units,
        Result = QualityResult.Aprobada,
        InspectionDate = DateOnly.FromDateTime(DateTime.Today)
    };

    [Fact]
    public async Task GetRecordsAsync_Instructor_OnlySeesAssignedOrders()
    {
        _quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Record(1, "OP-101"), Record(2, "OP-102")]);
        _orderService.Setup(s => s.CanAccessOrderAsync(1, 10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _orderService.Setup(s => s.CanAccessOrderAsync(2, 10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut().GetRecordsAsync(10, UserRoles.Instructor, "Laura");

        Assert.Single(result);
        Assert.Equal("OP-101", result[0].OrderNumber);
    }

    [Fact]
    public async Task GetRecordsAsync_Administrador_SeesAll()
    {
        _quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Record(1, "OP-101"), Record(2, "OP-102")]);
        _orderService.Setup(s => s.CanAccessOrderAsync(
                It.IsAny<int>(), 1, UserRoles.Administrador, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().GetRecordsAsync(1, UserRoles.Administrador, "Admin");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddRecordAsync_Instructor_OnAssignedOrder_Succeeds()
    {
        _orderService.Setup(s => s.CanAccessOrderAsync(1, 10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-101", Status = OrderStatus.EnProceso });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().AddRecordAsync(
            new CreateQualityRecordDto(1, 8, QualityResult.Aprobada),
            10, UserRoles.Instructor, "Laura");

        Assert.True(result.Success);
        _quality.Verify(r => r.AddAsync(It.IsAny<QualityRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRecordAsync_Instructor_OnForeignOrder_Fails()
    {
        _orderService.Setup(s => s.CanAccessOrderAsync(2, 10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut().AddRecordAsync(
            new CreateQualityRecordDto(2, 8, QualityResult.Aprobada),
            10, UserRoles.Instructor, "Laura");

        Assert.False(result.Success);
        Assert.Contains("acceso", result.Message, StringComparison.OrdinalIgnoreCase);
        _quality.Verify(r => r.AddAsync(It.IsAny<QualityRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CanAccessOrderAsync_Instructor_OnlyAssignedViaStage()
    {
        var orders = new Mock<IProductionOrderRepository>();
        var boms = new Mock<IBomRepository>();
        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        var requirements = new Mock<IOrderMaterialRequirementRepository>();
        var flowRepo = new Mock<IProductionFlowRepository>();
        var flowService = new Mock<IProductionFlowService>();
        var changeLogs = new Mock<IOrderChangeLogRepository>();
        var fichas = new Mock<IFichaRepository>();
        var uow = new Mock<IUnitOfWork>();
        var materials = new Mock<IMaterialRepository>();

        orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ProductionOrder { Id = 1, OrderNumber = "OP-101", ProductName = "A", Status = OrderStatus.EnProceso },
            new ProductionOrder { Id = 2, OrderNumber = "OP-102", ProductName = "B", Status = OrderStatus.EnProceso }
        ]);
        flowRepo.Setup(r => r.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductionOrderStage { ProductionOrderId = 1, InstructorUserId = 10 }]);
        flowRepo.Setup(r => r.GetStagesByOrderAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductionOrderStage { ProductionOrderId = 2, InstructorUserId = 99 }]);
        fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new ProductionOrderService(
            orders.Object, boms.Object, snapshots.Object, requirements.Object,
            flowRepo.Object, flowService.Object, changeLogs.Object, fichas.Object, uow.Object,
            new ProductionConsumptionService(boms.Object, materials.Object));

        Assert.True(await sut.CanAccessOrderAsync(1, 10, UserRoles.Instructor, "Laura"));
        Assert.False(await sut.CanAccessOrderAsync(2, 10, UserRoles.Instructor, "Laura"));
        Assert.True(await sut.CanAccessOrderAsync(2, 1, UserRoles.Administrador, "Admin"));
    }

    [Fact]
    public async Task CalidadController_Create_ReturnsForbidWhenOrderNotAccessible()
    {
        var quality = new Mock<IQualityService>();
        var orders = new Mock<IProductionOrderService>();
        orders.Setup(s => s.CanAccessOrderAsync(99, 10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = new CalidadController(quality.Object, orders.Object);
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim(ClaimTypes.Role, UserRoles.Instructor),
            new Claim(ClaimTypes.Name, "Laura")
        ], "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        var result = await controller.Create(
            new CreateQualityForm { ProductionOrderId = 99, Units = 1, Result = QualityResult.Aprobada },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        quality.Verify(
            s => s.AddRecordAsync(
                It.IsAny<CreateQualityRecordDto>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
