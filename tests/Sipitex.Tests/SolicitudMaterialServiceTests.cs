using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class SolicitudMaterialServiceTests
{
    private readonly Mock<ISolicitudMaterialRepository> _solicitudes = new();
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<ICodigoGeneradorService> _codigos = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private SolicitudMaterialService CreateSut() => new(
        _solicitudes.Object,
        _fichas.Object,
        _materials.Object,
        _codigos.Object,
        _uow.Object);

    private static Ficha FichaConInstructor(int fichaId, int instructorUserId) => new()
    {
        Id = fichaId,
        FichaCode = "FICHA-1",
        ProcessName = "Corte",
        InstructorName = "Laura",
        InstructorUserId = instructorUserId,
        Instructors =
        [
            new FichaInstructor { FichaId = fichaId, UserId = instructorUserId }
        ]
    };

    [Fact]
    public async Task CreateAsync_InstructorAsignado_CreaPendienteConCodigoYDetalles()
    {
        var ficha = FichaConInstructor(1, 10);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _materials.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 5, Name = "Tela", Stock = 100 });
        _codigos.Setup(c => c.GenerarCodigoSolicitudMaterialAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SOL-0007");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        SolicitudMaterial? saved = null;
        _solicitudes
            .Setup(r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<SolicitudMaterial, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(1, [new CreateDetalleSolicitudDto(5, 12)]),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("SOL-0007", saved!.Codigo);
        Assert.Equal(SolicitudMaterialEstado.Pendiente, saved.Estado);
        Assert.Equal(10, saved.SolicitanteId);
        Assert.Equal(1, saved.FichaId);
        Assert.Single(saved.Detalles);
        Assert.Equal(DetalleSolicitudEstado.Pendiente, saved.Detalles.First().EstadoItem);
        Assert.Null(saved.Detalles.First().CantidadAprobada);
        Assert.Equal(12m, saved.Detalles.First().CantidadSolicitada);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SinMaterialesConCantidad_Falla()
    {
        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(1, [new CreateDetalleSolicitudDto(5, 0)]),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.False(result.Success);
        _solicitudes.Verify(
            r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InstructorNoAsignado_Falla()
    {
        var ficha = FichaConInstructor(1, instructorUserId: 99);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(1, [new CreateDetalleSolicitudDto(5, 3)]),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.False(result.Success);
        _solicitudes.Verify(
            r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Administrador_PuedeEnCualquierFicha()
    {
        var ficha = FichaConInstructor(1, instructorUserId: 99);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _materials.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 5, Name = "Tela" });
        _codigos.Setup(c => c.GenerarCodigoSolicitudMaterialAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SOL-0001");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _solicitudes
            .Setup(r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(1, [new CreateDetalleSolicitudDto(5, 2)]),
            solicitanteId: 1,
            actorRole: UserRoles.Administrador,
            actorName: "Admin");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetListAsync_Instructor_SoloSusSolicitudes()
    {
        _solicitudes.Setup(r => r.GetAllWithFichaAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new SolicitudMaterial
            {
                Id = 1,
                Codigo = "SOL-0001",
                SolicitanteId = 10,
                Estado = SolicitudMaterialEstado.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                Ficha = new Ficha { FichaCode = "F1" },
                Solicitante = new User { Nombre = "Laura" }
            },
            new SolicitudMaterial
            {
                Id = 2,
                Codigo = "SOL-0002",
                SolicitanteId = 20,
                Estado = SolicitudMaterialEstado.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                Ficha = new Ficha { FichaCode = "F2" },
                Solicitante = new User { Nombre = "Carlos" }
            }
        ]);

        var list = await CreateSut().GetListAsync(10, UserRoles.Instructor);

        Assert.Single(list);
        Assert.Equal("SOL-0001", list[0].Codigo);
    }

    [Fact]
    public async Task GetDetailAsync_InstructorAjeno_DevuelveNull()
    {
        _solicitudes.Setup(r => r.GetByIdWithDetallesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new SolicitudMaterial
            {
                Id = 1,
                Codigo = "SOL-0001",
                SolicitanteId = 20,
                Estado = SolicitudMaterialEstado.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                Ficha = new Ficha { FichaCode = "F1" },
                Solicitante = new User { Nombre = "Carlos" },
                Detalles = []
            });

        var detail = await CreateSut().GetDetailAsync(1, 10, UserRoles.Instructor);

        Assert.Null(detail);
    }
}
