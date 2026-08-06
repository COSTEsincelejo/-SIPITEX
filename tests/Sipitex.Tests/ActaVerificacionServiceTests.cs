using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class ActaVerificacionServiceTests
{
    private readonly Mock<IActaVerificacionRepository> _actas = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ActaVerificacionService CreateSut() =>
        new(_actas.Object, _orders.Object, _fichas.Object, _uow.Object);

    private static Ficha OwnFicha(int fichaId = 1, int instructorId = 10) =>
        new()
        {
            Id = fichaId,
            FichaCode = "F1",
            InstructorName = "Laura Gómez",
            InstructorUserId = instructorId,
            Instructors = [new FichaInstructor { FichaId = fichaId, UserId = instructorId }]
        };

    private static ActaVerificacion DraftActa(
        int instructorId = 10,
        bool checklistOk = false,
        bool firmado = false)
    {
        var ficha = OwnFicha(1, instructorId);
        return new ActaVerificacion
        {
            Id = 5,
            ProductionOrderId = 1,
            ProductionOrder = new ProductionOrder { Id = 1, OrderNumber = "OP-001", ProductName = "Camisa" },
            FichaId = ficha.Id,
            Ficha = ficha,
            InstructorId = instructorId,
            Instructor = new User { Id = instructorId, Nombre = "Laura Gómez", Rol = UserRoles.Instructor },
            Observacion = "Prenda en buen estado",
            CumpleEspecificaciones = checklistOk,
            CumpleAcabados = checklistOk,
            CumpleSinDefectos = checklistOk,
            ChecklistCumpleRequisitos = checklistOk,
            Firmado = firmado,
            FechaObservacion = DateTime.UtcNow,
            FechaFirma = firmado ? DateTime.UtcNow : null,
            NombreFirmante = firmado ? "Laura Gómez" : null
        };
    }

    private static GuardarActaVerificacionDto SaveDto(bool checklistOk = true) =>
        new(1, 1, "Observación de prueba", checklistOk, checklistOk, checklistOk, checklistOk);

    [Fact]
    public async Task FirmarAsync_SinChecklistCompleto_NoPermiteFirmar()
    {
        var acta = DraftActa(checklistOk: false);
        _actas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(acta);

        var result = await CreateSut().FirmarAsync(5, 10, UserRoles.Instructor, "Laura Gómez");

        Assert.False(result.Success);
        Assert.Contains("checklist", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(acta.Firmado);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FirmarAsync_ChecklistCompleto_FirmaYGuardaSnapshot()
    {
        var acta = DraftActa(checklistOk: true);
        _actas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(acta);

        var result = await CreateSut().FirmarAsync(5, 10, UserRoles.Instructor, "Laura Gómez");

        Assert.True(result.Success);
        Assert.True(acta.Firmado);
        Assert.NotNull(acta.FechaFirma);
        Assert.Equal("Laura Gómez", acta.NombreFirmante);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_TrasFirma_BloqueaEdicion()
    {
        var acta = DraftActa(checklistOk: true, firmado: true);
        _actas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(acta);

        var result = await CreateSut().UpdateAsync(5, SaveDto(), 10, UserRoles.Instructor, "Laura Gómez");

        Assert.False(result.Success);
        Assert.Contains("firmada", result.Message, StringComparison.OrdinalIgnoreCase);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FirmarAsync_Administrador_NoPuedeFirmar()
    {
        var acta = DraftActa(checklistOk: true);
        _actas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(acta);

        var result = await CreateSut().FirmarAsync(5, 1, UserRoles.Administrador, "Admin");

        Assert.False(result.Success);
        Assert.Contains("administrador", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(acta.Firmado);
    }

    [Fact]
    public async Task CreateAsync_InstructorDeOtraFicha_Rechaza()
    {
        _orders.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 1, OrderNumber = "OP-001" });
        _fichas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OwnFicha(fichaId: 2, instructorId: 20));

        var result = await CreateSut().CreateAsync(
            new GuardarActaVerificacionDto(1, 2, "Obs", true, true, true, true),
            actorUserId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura Gómez");

        Assert.False(result.Success);
        Assert.Contains("propias fichas", result.Message, StringComparison.OrdinalIgnoreCase);
        _actas.Verify(r => r.AddAsync(It.IsAny<ActaVerificacion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetActasAsync_Instructor_SoloVeLasDeSuFicha()
    {
        var own = DraftActa(instructorId: 10);
        var other = DraftActa(instructorId: 20);
        other.Id = 6;
        other.Ficha = OwnFicha(2, 20);
        other.FichaId = 2;
        other.Instructor = new User { Id = 20, Nombre = "Carlos", Rol = UserRoles.Instructor };

        _actas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([own, other]);

        var result = await CreateSut().GetActasAsync(10, UserRoles.Instructor, "Laura Gómez");

        Assert.Single(result);
        Assert.Equal(5, result[0].Id);
    }

    [Fact]
    public async Task GetActasAsync_Administrador_VeTodasPeroNoPuedeEditar()
    {
        var own = DraftActa(instructorId: 10);
        var other = DraftActa(instructorId: 20);
        other.Id = 6;
        other.Ficha = OwnFicha(2, 20);
        other.FichaId = 2;
        other.Instructor = new User { Id = 20, Nombre = "Carlos", Rol = UserRoles.Instructor };

        _actas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([own, other]);

        var result = await CreateSut().GetActasAsync(1, UserRoles.Administrador, "Admin");

        Assert.Equal(2, result.Count);
        Assert.All(result, a =>
        {
            Assert.False(a.PuedeEditar);
            Assert.False(a.PuedeFirmarse);
        });
    }
}
