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
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IBodegaRepository> _bodegas = new();
    private readonly Mock<ICodigoGeneradorService> _codigos = new();
    private readonly Mock<IAlertService> _alerts = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private SolicitudMaterialService CreateSut()
    {
        _bodegas
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bodega { Id = 1, Nombre = "Bodega 1" });
        _bodegas
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bodega { Id = 2, Nombre = "Bodega 2" });

        _alerts
            .Setup(a => a.NotifyUsersAsync(
                It.IsAny<AlertType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<int>?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new(
            _solicitudes.Object,
            _fichas.Object,
            _orders.Object,
            _materials.Object,
            _bodegas.Object,
            _codigos.Object,
            _alerts.Object,
            _uow.Object);
    }

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
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 12)], BodegaId: 1),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("SOL-0007", saved!.Codigo);
        Assert.Equal(SolicitudMaterialEstado.Pendiente, saved.Estado);
        Assert.Equal(10, saved.SolicitanteId);
        Assert.Equal(1, saved.FichaId);
        Assert.Equal(SolicitudMaterialTipo.PorFicha, saved.Tipo);
        Assert.Single(saved.Detalles);
        Assert.Equal(DetalleSolicitudEstado.Pendiente, saved.Detalles.First().EstadoItem);
        Assert.Null(saved.Detalles.First().CantidadAprobada);
        Assert.Equal(12m, saved.Detalles.First().CantidadSolicitada);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _alerts.Verify(a => a.NotifyUsersAsync(
            AlertType.SolicitudMaterialNueva,
            It.IsAny<string>(),
            It.IsAny<string>(),
            null,
            UserRoles.Bodeguero,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SinMaterialesConCantidad_Falla()
    {
        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 0)], BodegaId: 1),
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
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 3)], BodegaId: 1),
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
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 2)], BodegaId: 1),
            solicitanteId: 1,
            actorRole: UserRoles.Administrador,
            actorName: "Admin");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_PorFicha_SinFichaId_FallaExplicitamente()
    {
        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(
                SolicitudMaterialTipo.PorFicha,
                FichaId: null,
                ProductionOrderId: null,
                DescripcionLibre: null,
                [new CreateDetalleSolicitudDto(5, 2)],
                BodegaId: 1),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.False(result.Success);
        Assert.Contains("ficha", result.Message!, StringComparison.OrdinalIgnoreCase);
        _solicitudes.Verify(
            r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PorFicha_SinMaterialId_FallaExplicitamente()
    {
        var ficha = FichaConInstructor(1, 10);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(
                SolicitudMaterialTipo.PorFicha,
                1,
                null,
                null,
                [new CreateDetalleSolicitudDto(null, 2, "solo texto")],
                BodegaId: 1),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.False(result.Success);
        _solicitudes.Verify(
            r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InsumosLibres_SinOrdenNiFicha_Exito()
    {
        _codigos.Setup(c => c.GenerarCodigoSolicitudMaterialAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SOL-LIB-1");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        SolicitudMaterial? saved = null;
        _solicitudes
            .Setup(r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<SolicitudMaterial, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(
                SolicitudMaterialTipo.InsumosLibres,
                FichaId: null,
                ProductionOrderId: null,
                DescripcionLibre: "Pedido taller",
                [new CreateDetalleSolicitudDto(null, 3, "Cremallera nylon #5")],
                BodegaId: 1),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal(SolicitudMaterialTipo.InsumosLibres, saved!.Tipo);
        Assert.Null(saved.FichaId);
        Assert.Null(saved.ProductionOrderId);
        Assert.Equal("Pedido taller", saved.DescripcionLibre);
        Assert.Null(saved.Detalles.Single().MaterialId);
        Assert.Equal("Cremallera nylon #5", saved.Detalles.Single().DescripcionItem);
        Assert.Equal(3m, saved.Detalles.Single().CantidadSolicitada);
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

    [Fact]
    public async Task CreateAsync_MaterialDeOtraBodega_Falla()
    {
        var ficha = FichaConInstructor(1, 10);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _materials.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 5, Name = "Tela", BodegaId = 2 });

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(
                SolicitudMaterialTipo.PorFicha, 1, null, null,
                [new CreateDetalleSolicitudDto(5, 12)],
                BodegaId: 1),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.False(result.Success);
        Assert.Contains("no pertenece a la bodega", result.Message, StringComparison.OrdinalIgnoreCase);
        _solicitudes.Verify(
            r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetListForBodegaAsync_SoloDevuelveDeEsaBodega()
    {
        _solicitudes.Setup(r => r.GetAllWithFichaAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new SolicitudMaterial
            {
                Id = 1,
                Codigo = "SOL-B1",
                BodegaId = 1,
                SolicitanteId = 10,
                Estado = SolicitudMaterialEstado.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                Ficha = new Ficha { FichaCode = "F1" },
                Solicitante = new User { Nombre = "Laura" }
            },
            new SolicitudMaterial
            {
                Id = 2,
                Codigo = "SOL-B2",
                BodegaId = 2,
                SolicitanteId = 10,
                Estado = SolicitudMaterialEstado.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                Ficha = new Ficha { FichaCode = "F2" },
                Solicitante = new User { Nombre = "Laura" }
            }
        ]);

        var list = await CreateSut().GetListForBodegaAsync(bodegaId: 1);

        Assert.Single(list);
        Assert.Equal("SOL-B1", list[0].Codigo);
    }

    [Fact]
    public async Task GetResolucionDetailAsync_OtraBodega_DevuelveNull()
    {
        _solicitudes.Setup(r => r.GetByIdWithDetallesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new SolicitudMaterial
            {
                Id = 1,
                Codigo = "SOL-B2",
                BodegaId = 2,
                SolicitanteId = 10,
                Estado = SolicitudMaterialEstado.Pendiente,
                FechaSolicitud = DateTime.UtcNow,
                Ficha = new Ficha { FichaCode = "F1" },
                Solicitante = new User { Nombre = "Laura" },
                Detalles = []
            });

        var detail = await CreateSut().GetResolucionDetailAsync(1, bodegaId: 1);

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetListAndDetail_ConBodegaValida_PueblanBodegaNombre()
    {
        var solicitud = new SolicitudMaterial
        {
            Id = 1,
            Codigo = "SOL-0001",
            BodegaId = 1,
            Bodega = new Bodega { Id = 1, Nombre = "Bodega 1" },
            SolicitanteId = 10,
            Estado = SolicitudMaterialEstado.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            Ficha = new Ficha { FichaCode = "F1" },
            Solicitante = new User { Nombre = "Laura" },
            Detalles = []
        };

        _solicitudes.Setup(r => r.GetAllWithFichaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([solicitud]);
        _solicitudes.Setup(r => r.GetByIdWithDetallesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);

        var sut = CreateSut();
        var list = await sut.GetListAsync(10, UserRoles.Instructor);
        var listBodega = await sut.GetListForBodegaAsync(bodegaId: 1);
        var detail = await sut.GetDetailAsync(1, 10, UserRoles.Instructor);

        Assert.Equal("Bodega 1", Assert.Single(list).BodegaNombre);
        Assert.Equal("Bodega 1", Assert.Single(listBodega).BodegaNombre);
        Assert.NotNull(detail);
        Assert.Equal("Bodega 1", detail!.BodegaNombre);
        Assert.NotEqual("—", detail.BodegaNombre);
        Assert.False(string.IsNullOrWhiteSpace(detail.BodegaNombre));
    }
}
