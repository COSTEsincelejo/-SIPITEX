using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class FichaInstructorAssignmentTests
{
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionSessionRepository> _sessions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProductionOrderService> _orderService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private FichaService CreateSut() =>
        new(_fichas.Object, _orders.Object, _sessions.Object, _users.Object, _orderService.Object, _uow.Object);

    private static User Instructor(int id, string name, bool active = true) => new()
    {
        Id = id,
        Nombre = name,
        Email = $"{name.Replace(" ", "").ToLowerInvariant()}@test.local",
        Rol = UserRoles.Instructor,
        IsActive = active,
        PasswordHash = "x"
    };

    [Fact]
    public async Task CreateFichaAsync_WithRegisteredInstructors_Succeeds()
    {
        _fichas.Setup(r => r.ExistsByCodeAsync("FICHA-N1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(Instructor(10, "Laura Gómez"));
        _users.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(Instructor(20, "Carlos Méndez"));

        var result = await CreateSut().CreateFichaAsync(
            new CreateFichaDto("FICHA-N1", "Corte", [10, 20], "Mañana"));

        Assert.True(result.Success);
        _fichas.Verify(r => r.AddAsync(
            It.Is<Ficha>(f =>
                f.FichaCode == "FICHA-N1"
                && f.Instructors.Count == 2
                && f.InstructorUserId == 10
                && f.InstructorName.Contains("Carlos")
                && f.InstructorName.Contains("Laura")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFichaAsync_WhenUserIsNotInstructor_Fails()
    {
        _fichas.Setup(r => r.ExistsByCodeAsync("FICHA-N1", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = 1,
            Nombre = "Admin",
            Rol = UserRoles.Administrador,
            IsActive = true,
            PasswordHash = "x"
        });

        var result = await CreateSut().CreateFichaAsync(
            new CreateFichaDto("FICHA-N1", "Corte", [1], "Mañana"));

        Assert.False(result.Success);
        Assert.Contains("rol Instructor", result.Message);
        _fichas.Verify(r => r.AddAsync(It.IsAny<Ficha>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignInstructorAsync_AddsSecondInstructor()
    {
        var ficha = new Ficha
        {
            Id = 5,
            FichaCode = "FICHA-T1",
            InstructorUserId = 10,
            InstructorName = "Laura Gómez",
            Instructors =
            [
                new FichaInstructor { FichaId = 5, UserId = 10, User = Instructor(10, "Laura Gómez") }
            ]
        };
        _fichas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);
        _users.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(Instructor(20, "Carlos Méndez"));

        var result = await CreateSut().AssignInstructorAsync(
            5, 20, actorUserId: 1, actorRole: UserRoles.Administrador);

        Assert.True(result.Success);
        Assert.Equal(2, ficha.Instructors.Count);
        Assert.Contains(ficha.Instructors, i => i.UserId == 20);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveInstructorAsync_KeepsAtLeastOne()
    {
        var ficha = new Ficha
        {
            Id = 5,
            FichaCode = "FICHA-T1",
            InstructorUserId = 10,
            Instructors =
            [
                new FichaInstructor { FichaId = 5, UserId = 10, User = Instructor(10, "Laura Gómez") }
            ]
        };
        _fichas.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(ficha);

        var result = await CreateSut().RemoveInstructorAsync(
            5, 10, actorUserId: 1, actorRole: UserRoles.Administrador);

        Assert.False(result.Success);
        Assert.Contains("al menos un instructor", result.Message);
    }

    [Fact]
    public async Task GetFichasAsync_Instructor_SeesFichaViaM2MMembership()
    {
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new Ficha
            {
                Id = 1,
                FichaCode = "F1",
                ProcessName = "Trazo",
                InstructorUserId = 99,
                InstructorName = "Otro",
                Instructors =
                [
                    new FichaInstructor { UserId = 10, User = Instructor(10, "Laura Gómez") },
                    new FichaInstructor { UserId = 99, User = Instructor(99, "Otro") }
                ]
            },
            new Ficha
            {
                Id = 2,
                FichaCode = "F2",
                InstructorUserId = 20,
                Instructors = [new FichaInstructor { UserId = 20, User = Instructor(20, "Carlos") }]
            }
        ]);

        var result = await CreateSut().GetFichasAsync(10, UserRoles.Instructor, "Laura Gómez");

        Assert.Single(result);
        Assert.Equal("F1", result[0].FichaCode);
        Assert.Equal(2, result[0].Instructors!.Count);
    }
}
