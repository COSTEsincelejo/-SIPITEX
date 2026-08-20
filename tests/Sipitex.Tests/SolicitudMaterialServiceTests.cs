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
    private readonly Mock<IUserRepository> _users = new();
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
        _users
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
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
            _users.Object,
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
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 12)]),
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
    }

    [Fact]
    public async Task CreateAsync_SinMaterialesConCantidad_Falla()
    {
        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 0)]),
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
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 3)]),
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
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 2)]),
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
                [new CreateDetalleSolicitudDto(5, 2)]),
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
                [new CreateDetalleSolicitudDto(null, 2, "solo texto")]),
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
                [new CreateDetalleSolicitudDto(null, 3, "Cremallera nylon #5")]),
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
        Assert.Equal(1, saved.BodegaId);
    }

    [Fact]
    public async Task CreateAsync_PorFicha_MaterialBodega1_AsignaBodegaId1()
    {
        var ficha = FichaConInstructor(1, 10);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _materials.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 5, Name = "Tela", Stock = 100, BodegaId = 1 });
        _codigos.Setup(c => c.GenerarCodigoSolicitudMaterialAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SOL-B1");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        SolicitudMaterial? saved = null;
        _solicitudes
            .Setup(r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<SolicitudMaterial, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 12)]),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal(1, saved!.BodegaId);
    }

    [Fact]
    public async Task CreateAsync_PorFicha_MaterialBodega2_AsignaBodegaId2()
    {
        var ficha = FichaConInstructor(1, 10);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _materials.Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 8, Name = "Hilo", Stock = 40, BodegaId = 2 });
        _codigos.Setup(c => c.GenerarCodigoSolicitudMaterialAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SOL-B2");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        SolicitudMaterial? saved = null;
        _solicitudes
            .Setup(r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<SolicitudMaterial, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(8, 4)]),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.BodegaId);
    }

    [Fact]
    public async Task CreateAsync_PorFicha_MaterialesDeBodegasMixtas_Falla()
    {
        var ficha = FichaConInstructor(1, 10);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _materials.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 5, Name = "Tela", BodegaId = 1 });
        _materials.Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 8, Name = "Hilo", BodegaId = 2 });

        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(
                SolicitudMaterialTipo.PorFicha,
                1,
                null,
                null,
                [new CreateDetalleSolicitudDto(5, 2), new CreateDetalleSolicitudDto(8, 3)]),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.False(result.Success);
        Assert.Contains("misma bodega", result.Message, StringComparison.OrdinalIgnoreCase);
        _solicitudes.Verify(
            r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InsumosLibres_SinBodegaId_UsaFallbackBodega1()
    {
        _codigos.Setup(c => c.GenerarCodigoSolicitudMaterialAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SOL-LIB-FB");
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
                DescripcionLibre: "Pedido",
                [new CreateDetalleSolicitudDto(null, 1, "Botón")],
                BodegaId: null),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.True(result.Success, result.Message);
        Assert.Equal(1, saved!.BodegaId);
    }

    [Fact]
    public async Task GetListForBodegaAsync_FiltraPorBodegaDelBodeguero()
    {
        _solicitudes.Setup(r => r.GetAllWithFichaAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            ListSolicitud(1, "SOL-B1", bodegaId: 1),
            ListSolicitud(2, "SOL-B2", bodegaId: 2)
        ]);

        var deBodega1 = await CreateSut().GetListForBodegaAsync(viewerBodegaId: 1);
        var deBodega2 = await CreateSut().GetListForBodegaAsync(viewerBodegaId: 2);

        Assert.Single(deBodega1);
        Assert.Equal("SOL-B1", deBodega1[0].Codigo);
        Assert.DoesNotContain(deBodega1, s => s.Codigo == "SOL-B2");

        Assert.Single(deBodega2);
        Assert.Equal("SOL-B2", deBodega2[0].Codigo);
        Assert.DoesNotContain(deBodega2, s => s.Codigo == "SOL-B1");
    }

    [Fact]
    public async Task GetListForBodegaAsync_ViewerSinBodega_ListaVacia()
    {
        _solicitudes.Setup(r => r.GetAllWithFichaAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            ListSolicitud(1, "SOL-B1", bodegaId: 1)
        ]);

        var list = await CreateSut().GetListForBodegaAsync(viewerBodegaId: null);

        Assert.Empty(list);
        _solicitudes.Verify(r => r.GetAllWithFichaAsync(It.IsAny<CancellationToken>()), Times.Never);
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

        var detail = await CreateSut().GetResolucionDetailAsync(1, viewerBodegaId: 1);

        Assert.Null(detail);
    }

    private static SolicitudMaterial ListSolicitud(int id, string codigo, int bodegaId) =>
        new()
        {
            Id = id,
            Codigo = codigo,
            BodegaId = bodegaId,
            SolicitanteId = 10,
            Estado = SolicitudMaterialEstado.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            Ficha = new Ficha { FichaCode = "F1" },
            Solicitante = new User { Nombre = "Laura" }
        };

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
    public async Task CreateAsync_InsumosLibres_BodegaIdInexistente_FallaSinExcepcion()
    {
        var result = await CreateSut().CreateAsync(
            new CreateSolicitudMaterialDto(
                SolicitudMaterialTipo.InsumosLibres,
                FichaId: null,
                ProductionOrderId: null,
                DescripcionLibre: "Pedido",
                [new CreateDetalleSolicitudDto(null, 1, "Botón")],
                BodegaId: 99),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.False(result.Success);
        Assert.Contains("Bodega no válida", result.Message, StringComparison.OrdinalIgnoreCase);
        _solicitudes.Verify(
            r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _codigos.Verify(c => c.GenerarCodigoSolicitudMaterialAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PorFicha_NotificaSoloBodeguerosDeEsaBodega()
    {
        var ficha = FichaConInstructor(1, 10);
        _fichas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _materials.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 5, Name = "Tela", Stock = 100, BodegaId = 2 });
        _codigos.Setup(c => c.GenerarCodigoSolicitudMaterialAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("SOL-NTF");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _solicitudes
            .Setup(r => r.AddAsync(It.IsAny<SolicitudMaterial>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        _users.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new User { Id = 21, Rol = UserRoles.Bodeguero, BodegaId = 1, IsActive = true },
            new User { Id = 22, Rol = UserRoles.Bodeguero, BodegaId = 2, IsActive = true },
            new User { Id = 23, Rol = UserRoles.Bodeguero, BodegaId = 2, IsActive = false }
        ]);

        var result = await sut.CreateAsync(
            new CreateSolicitudMaterialDto(SolicitudMaterialTipo.PorFicha, 1, null, null, [new CreateDetalleSolicitudDto(5, 12)]),
            solicitanteId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura");

        Assert.True(result.Success, result.Message);
        _alerts.Verify(a => a.NotifyUsersAsync(
            AlertType.SolicitudMaterialNueva,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<IReadOnlyList<int>>(ids => ids.Count == 1 && ids[0] == 22),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _alerts.Verify(a => a.NotifyUsersAsync(
            It.IsAny<AlertType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IReadOnlyList<int>?>(),
            UserRoles.Bodeguero,
            It.IsAny<CancellationToken>()), Times.Never);
    }
}

