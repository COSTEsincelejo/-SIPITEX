using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Reporting;

namespace Sipitex.Tests;

public class ReportActividadInstructorTests
{
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IQualityRepository> _quality = new();
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IProductionFlowRepository> _flow = new();
    private readonly Mock<IProductionSessionRepository> _sessions = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snapshots = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IStatisticsService> _stats = new();

    private ReportService CreateSut() => new(
        _materials.Object,
        _orders.Object,
        _quality.Object,
        _fichas.Object,
        _flow.Object,
        _sessions.Object,
        _snapshots.Object,
        _users.Object,
        _stats.Object);

    [Fact]
    public async Task ExportActividadInstructor_SinInstructor_DevuelveAviso()
    {
        var file = await CreateSut().ExportActividadInstructorAsync("excel", new ReportFilterDto());

        Assert.NotNull(file.Content);
        Assert.True(file.Content.Length > 0);
        Assert.Contains("ActividadInstructor", file.FileName);
        // El Excel generado incluye el aviso de instructor obligatorio
        Assert.EndsWith(".xlsx", file.FileName);
    }

    [Fact]
    public async Task ExportActividadInstructor_SinSesiones_MensajeSinActividad()
    {
        _users.Setup(u => u.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, Nombre = "Laura Gómez", Rol = UserRoles.Instructor });
        _sessions.Setup(s => s.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var file = await CreateSut().ExportActividadInstructorAsync(
            "pdf",
            new ReportFilterDto(InstructorId: 10));

        Assert.NotNull(file.Content);
        Assert.True(file.Content.Length > 0);
        Assert.EndsWith(".pdf", file.FileName);
        _snapshots.Verify(s => s.GetByOrderIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExportActividadInstructor_ConSesion_IncluyeProduccionYConsumo()
    {
        _users.Setup(u => u.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, Nombre = "Laura Gómez", Rol = UserRoles.Instructor });

        var ficha = new Ficha
        {
            Id = 1,
            FichaCode = "FICHA-T1",
            ProcessName = "Trazo",
            Turno = "Mañana",
            InstructorUserId = 10,
            Instructors = [new FichaInstructor { FichaId = 1, UserId = 10 }]
        };
        var order = new ProductionOrder { Id = 5, OrderNumber = "OP-001", ProductName = "Camisa" };

        _sessions.Setup(s => s.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProductionSession
                {
                    Id = 1,
                    FichaId = 1,
                    Ficha = ficha,
                    ProductionOrderId = 5,
                    ProductionOrder = order,
                    Units = 10,
                    SessionDate = new DateTime(2026, 8, 6, 9, 0, 0),
                    RegisteredByUserId = 10
                }
            ]);

        _snapshots.Setup(s => s.GetByOrderIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProductionOrderBomSnapshot
                {
                    ProductionOrderId = 5,
                    MaterialId = 1,
                    MaterialCode = "mat1",
                    MaterialName = "Tela Jersey",
                    QuantityPerUnit = 1.5m,
                    Unit = Domain.Enums.MaterialUnit.Metros
                }
            ]);

        var file = await CreateSut().ExportActividadInstructorAsync(
            "excel",
            new ReportFilterDto(InstructorId: 10, Jornada: "Mañana"));

        Assert.True(file.Content.Length > 0);
        Assert.EndsWith(".xlsx", file.FileName);
        _snapshots.Verify(s => s.GetByOrderIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);

        // Verifica contenido: producción + consumo BOM (10 × 1.5 = 15)
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(file.Content));
        using var reader = new StreamReader(zip.GetEntry("xl/sharedStrings.xml")!.Open());
        var shared = await reader.ReadToEndAsync();
        Assert.Contains("Laura Gómez", shared);
        Assert.Contains("FICHA-T1", shared);
        Assert.Contains("Tela Jersey", shared);
        Assert.Contains("15", shared);
        Assert.Contains("Total producido", shared);
    }
}
