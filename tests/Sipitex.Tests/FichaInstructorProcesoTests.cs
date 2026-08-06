using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class FichaInstructorProcesoTests
{
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionSessionRepository> _sessions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProductionOrderService> _orderService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private FichaService CreateSut() =>
        new(_fichas.Object, _orders.Object, _sessions.Object, _users.Object, _orderService.Object, _uow.Object);

    private static User Instructor(int id, string name) => new()
    {
        Id = id,
        Nombre = name,
        Email = $"{id}@test.local",
        Rol = UserRoles.Instructor,
        IsActive = true,
        PasswordHash = "x"
    };

    private static Ficha FichaWithInstructors() => new()
    {
        Id = 5,
        FichaCode = "FICHA-T1",
        ProcessName = "Trazo",
        InstructorUserId = 10,
        Instructors =
        [
            new FichaInstructor
            {
                FichaId = 5,
                UserId = 10,
                User = Instructor(10, "Laura Gómez"),
                Proceso = "Trazo"
            },
            new FichaInstructor
            {
                FichaId = 5,
                UserId = 20,
                User = Instructor(20, "Carlos Méndez"),
                Proceso = "Corte"
            }
        ]
    };

    [Fact]
    public async Task UpdateInstructorProceso_Admin_CanEditAnyAssignment()
    {
        var ficha = FichaWithInstructors();
        _fichas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        var result = await CreateSut().UpdateInstructorProcesoAsync(
            5, 20, "  Confección  ",
            actorUserId: 1,
            actorRole: UserRoles.Administrador);

        Assert.True(result.Success);
        Assert.Equal("Confección", ficha.Instructors.First(i => i.UserId == 20).Proceso);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateInstructorProceso_Instructor_CanEditOwnAssignment()
    {
        var ficha = FichaWithInstructors();
        _fichas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        var result = await CreateSut().UpdateInstructorProcesoAsync(
            5, 10, "Trazo fino",
            actorUserId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura Gómez");

        Assert.True(result.Success);
        Assert.Equal("Trazo fino", ficha.Instructors.First(i => i.UserId == 10).Proceso);
    }

    [Fact]
    public async Task UpdateInstructorProceso_Instructor_CannotEditOtherAssignment()
    {
        var ficha = FichaWithInstructors();
        _fichas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        var result = await CreateSut().UpdateInstructorProcesoAsync(
            5, 20, "Hack",
            actorUserId: 10,
            actorRole: UserRoles.Instructor,
            actorName: "Laura Gómez");

        Assert.False(result.Success);
        Assert.Contains("permiso", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Corte", ficha.Instructors.First(i => i.UserId == 20).Proceso);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
