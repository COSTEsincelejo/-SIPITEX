using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

/// <summary>
/// Estadisticas: Instructor acotado a sus órdenes (mismo criterio GetOrdersAsync / Reportes).
/// </summary>
public class EstadisticasInstructorScopeTests
{
    private readonly Mock<IProductionOrderService> _orders = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IQualityRepository> _quality = new();

    private StatisticsService CreateSut() => new(
        _orders.Object,
        _materials.Object,
        _quality.Object);

    private static ProductionOrderDto OrderDto(
        int id,
        string number,
        int produced,
        int total,
        OrderStatus status = OrderStatus.EnProceso) => new(
        id,
        number,
        "Camisa",
        total,
        produced,
        0,
        status,
        DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
        "");

    private static QualityRecord Quality(int orderId, int units, QualityResult result) => new()
    {
        Id = orderId * 10 + units,
        ProductionOrderId = orderId,
        UnitsInspected = units,
        Result = result,
        InspectionDate = DateOnly.FromDateTime(DateTime.Today)
    };

    [Fact]
    public async Task GetDashboardAsync_Instructor_OnlyUsesAssignedOrdersAndQuality()
    {
        _orders.Setup(s => s.GetOrdersAsync(10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync([OrderDto(1, "OP-101", produced: 8, total: 20)]);
        _materials.Setup(m => m.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Material { Id = 1, Name = "Tela", Stock = 1, MinStock = 5, Unit = MaterialUnit.Metros },
                new Material { Id = 2, Name = "Hilo", Stock = 10, MinStock = 2, Unit = MaterialUnit.Unidades }
            ]);
        _quality.Setup(q => q.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Quality(1, 5, QualityResult.Aprobada),   // propia
                Quality(2, 10, QualityResult.Aprobada)  // otra orden — debe ignorarse
            ]);

        var dash = await CreateSut().GetDashboardAsync(10, UserRoles.Instructor, "Laura");

        Assert.Equal(8, dash.TotalProduced);
        Assert.Equal(1, dash.ActiveOrders);
        Assert.Equal(100m, dash.QualityRate); // solo 5/5 de la orden 1
        Assert.Single(dash.ChartData);
        Assert.Equal("OP-101", dash.ChartData[0].Label);
        // Materiales: sin filtro por instructor (como Reportes dashboard)
        Assert.Equal(1, dash.LowStockCount);
    }

    [Fact]
    public async Task GetDashboardAsync_Administrador_SeesAllOrdersAndQuality()
    {
        _orders.Setup(s => s.GetOrdersAsync(1, UserRoles.Administrador, "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                OrderDto(1, "OP-101", produced: 8, total: 20),
                OrderDto(2, "OP-102", produced: 12, total: 30)
            ]);
        _materials.Setup(m => m.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Material { Id = 1, Name = "Tela", Stock = 1, MinStock = 5, Unit = MaterialUnit.Metros }
            ]);
        _quality.Setup(q => q.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Quality(1, 5, QualityResult.Aprobada),
                Quality(2, 5, QualityResult.Rechazada)
            ]);

        var dash = await CreateSut().GetDashboardAsync(1, UserRoles.Administrador, "Admin");

        Assert.Equal(20, dash.TotalProduced);
        Assert.Equal(2, dash.ActiveOrders);
        Assert.Equal(50m, dash.QualityRate); // 5 aprobadas / 10 inspeccionadas
        Assert.Equal(2, dash.ChartData.Count);
        Assert.Equal(1, dash.LowStockCount);
    }

    [Fact]
    public async Task EstadisticasController_Index_Instructor_PassesSelfViewer()
    {
        var stats = new Mock<IStatisticsService>();
        stats.Setup(s => s.GetDashboardAsync(10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardKpiDto(3, 100m, 1, 0, 0, []));

        var controller = new EstadisticasController(stats.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "10"),
                        new Claim(ClaimTypes.Role, UserRoles.Instructor),
                        new Claim(ClaimTypes.Name, "Laura")
                    ], "Test"))
                }
            }
        };

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<EstadisticasIndexViewModel>(view.Model);
        Assert.Equal(3, vm.Dashboard.TotalProduced);

        stats.Verify(s => s.GetDashboardAsync(
            10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()), Times.Once);
        stats.Verify(s => s.GetDashboardAsync(
            null, null, null, It.IsAny<CancellationToken>()), Times.Never);
    }
}
