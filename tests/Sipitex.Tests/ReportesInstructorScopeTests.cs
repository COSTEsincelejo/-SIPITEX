using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Reporting;
using Sipitex.Web.Controllers;
using Sipitex.Web.Models;

namespace Sipitex.Tests;

/// <summary>
/// Gap #11 (AUDITORIA_ROLES_FUNCIONES): Instructor no puede ampliar reportes vía query string;
/// Admin/Bodeguero conservan alcance global; Inventario bloqueado al Instructor.
/// </summary>
public class ReportesInstructorScopeTests
{
    private readonly Mock<IReportService> _reports = new();
    private readonly Mock<IFichaService> _fichas = new();

    private static ReportFileDto DummyFile(string name = "Ordenes") => new(
        [0x25, 0x50, 0x44, 0x46], // %PDF
        "application/pdf",
        $"{name}.pdf");

    private ReportesController CreateController(ClaimsPrincipal user)
    {
        _reports.Setup(r => r.ExportOrdersAsync(
                It.IsAny<string>(), It.IsAny<ReportFilterDto?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DummyFile("Ordenes"));
        _reports.Setup(r => r.ExportQualityAsync(
                It.IsAny<string>(), It.IsAny<ReportFilterDto?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DummyFile("Calidad"));
        _reports.Setup(r => r.ExportDashboardAsync(
                It.IsAny<string>(), It.IsAny<ReportFilterDto?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DummyFile("Dashboard"));
        _reports.Setup(r => r.ExportInventoryAsync(
                It.IsAny<string>(), It.IsAny<ReportFilterDto?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DummyFile("Inventario"));
        _reports.Setup(r => r.ExportActividadInstructorAsync(
                It.IsAny<string>(), It.IsAny<ReportFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DummyFile("ActividadInstructor"));

        _fichas.Setup(f => f.GetActiveInstructorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new InstructorOptionDto(10, "Laura Gómez"),
                new InstructorOptionDto(20, "Carlos Méndez")
            ]);
        _fichas.Setup(f => f.GetFichasAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var controller = new ReportesController(_reports.Object, _fichas.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
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

    [Fact]
    public async Task Ordenes_Instructor_IgnoresForeignInstructorIdInQuery()
    {
        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));

        var result = await controller.Ordenes(
            format: "pdf",
            instructorId: 99, // intento de ver a otro
            cancellationToken: CancellationToken.None);

        Assert.IsType<FileContentResult>(result);
        _reports.Verify(r => r.ExportOrdersAsync(
            "pdf",
            It.Is<ReportFilterDto?>(f => f != null && f.InstructorId == 10),
            It.IsAny<CancellationToken>()), Times.Once);
        _reports.Verify(r => r.ExportOrdersAsync(
            It.IsAny<string>(),
            It.Is<ReportFilterDto?>(f => f != null && f.InstructorId == 99),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ordenes_Instructor_WithoutQuery_StillForcesOwnScope()
    {
        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));

        await controller.Ordenes(format: "excel", cancellationToken: CancellationToken.None);

        _reports.Verify(r => r.ExportOrdersAsync(
            "excel",
            It.Is<ReportFilterDto?>(f => f != null && f.InstructorId == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calidad_Dashboard_Actividad_Instructor_ForceSelf()
    {
        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));

        await controller.Calidad(instructorId: 20, cancellationToken: CancellationToken.None);
        await controller.Dashboard(instructorId: 20, cancellationToken: CancellationToken.None);
        await controller.ActividadInstructor(instructorId: 20, cancellationToken: CancellationToken.None);

        _reports.Verify(r => r.ExportQualityAsync(
            It.IsAny<string>(),
            It.Is<ReportFilterDto?>(f => f!.InstructorId == 10),
            It.IsAny<CancellationToken>()), Times.Once);
        _reports.Verify(r => r.ExportDashboardAsync(
            It.IsAny<string>(),
            It.Is<ReportFilterDto?>(f => f!.InstructorId == 10),
            It.IsAny<CancellationToken>()), Times.Once);
        _reports.Verify(r => r.ExportActividadInstructorAsync(
            It.IsAny<string>(),
            It.Is<ReportFilterDto>(f => f.InstructorId == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inventario_Instructor_ReturnsForbid()
    {
        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));

        var result = await controller.Inventario(cancellationToken: CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        _reports.Verify(r => r.ExportInventoryAsync(
            It.IsAny<string>(), It.IsAny<ReportFilterDto?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Inventario_Administrador_AllowsGlobalExport()
    {
        var controller = CreateController(Principal(1, UserRoles.Administrador, "Admin"));

        var result = await controller.Inventario(format: "pdf", cancellationToken: CancellationToken.None);

        Assert.IsType<FileContentResult>(result);
        _reports.Verify(r => r.ExportInventoryAsync(
            "pdf", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Inventario_Bodeguero_AllowsGlobalExport()
    {
        var controller = CreateController(Principal(3, UserRoles.Bodeguero, "Bodega"));

        var result = await controller.Inventario(format: "excel", cancellationToken: CancellationToken.None);

        Assert.IsType<FileContentResult>(result);
        _reports.Verify(r => r.ExportInventoryAsync(
            "excel", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ordenes_Administrador_KeepsOptionalInstructorFilter()
    {
        var controller = CreateController(Principal(1, UserRoles.Administrador, "Admin"));

        await controller.Ordenes(instructorId: 20, cancellationToken: CancellationToken.None);
        await controller.Ordenes(cancellationToken: CancellationToken.None);

        _reports.Verify(r => r.ExportOrdersAsync(
            It.IsAny<string>(),
            It.Is<ReportFilterDto?>(f => f != null && f.InstructorId == 20),
            It.IsAny<CancellationToken>()), Times.Once);
        _reports.Verify(r => r.ExportOrdersAsync(
            It.IsAny<string>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ordenes_Bodeguero_KeepsGlobalWhenNoFilter()
    {
        var controller = CreateController(Principal(3, UserRoles.Bodeguero, "Bodega"));

        await controller.Ordenes(cancellationToken: CancellationToken.None);

        _reports.Verify(r => r.ExportOrdersAsync(
            It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Index_Instructor_ScopesInstructorsAndFichas()
    {
        var controller = CreateController(Principal(10, UserRoles.Instructor, "Laura"));

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ReportesIndexViewModel>(view.Model);
        Assert.True(model.IsInstructorScoped);
        Assert.Equal(10, model.ForcedInstructorId);
        Assert.Single(model.Instructors);
        Assert.Equal(10, model.Instructors[0].Id);
        _fichas.Verify(f => f.GetFichasAsync(
            10, UserRoles.Instructor, "Laura", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportOrders_IncludesStageAssignedOrders_ForInstructorFilter()
    {
        var materials = new Mock<IMaterialRepository>();
        var orders = new Mock<IProductionOrderRepository>();
        var quality = new Mock<IQualityRepository>();
        var fichas = new Mock<IFichaRepository>();
        var flow = new Mock<IProductionFlowRepository>();
        var sessions = new Mock<IProductionSessionRepository>();
        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        var users = new Mock<IUserRepository>();
        var stats = new Mock<IStatisticsService>();

        orders.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ProductionOrder
            {
                Id = 1, OrderNumber = "OP-101", ProductName = "A",
                TotalQuantity = 10, ProducedQuantity = 2, Status = OrderStatus.EnProceso,
                Deadline = new DateOnly(2026, 12, 1)
            },
            new ProductionOrder
            {
                Id = 2, OrderNumber = "OP-102", ProductName = "B",
                TotalQuantity = 10, ProducedQuantity = 0, Status = OrderStatus.EnProceso,
                Deadline = new DateOnly(2026, 12, 1)
            }
        ]);
        // Sin fichas: el alcance debe venir solo de etapa MES
        fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        flow.Setup(r => r.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductionOrderStage { ProductionOrderId = 1, InstructorUserId = 10 }]);
        flow.Setup(r => r.GetStagesByOrderAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProductionOrderStage { ProductionOrderId = 2, InstructorUserId = 99 }]);

        var sut = new ReportService(
            materials.Object, orders.Object, quality.Object, fichas.Object, flow.Object,
            sessions.Object, snapshots.Object, users.Object, stats.Object);

        var file = await sut.ExportOrdersAsync("excel", new ReportFilterDto(InstructorId: 10));

        Assert.NotNull(file.Content);
        Assert.True(file.Content.Length > 0);
        using var ms = new MemoryStream(file.Content);
        using var wb = new ClosedXML.Excel.XLWorkbook(ms);
        var ws = wb.Worksheets.First();
        var text = string.Join('\n', ws.RowsUsed().Select(r =>
            string.Join('|', r.CellsUsed().Select(c => c.GetString()))));
        Assert.Contains("OP-101", text);
        Assert.DoesNotContain("OP-102", text);
        flow.Verify(r => r.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        flow.Verify(r => r.GetStagesByOrderAsync(2, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
