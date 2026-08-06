using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class FichaServiceScopeTests
{
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionSessionRepository> _sessions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProductionOrderService> _orderService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private FichaService CreateSut() =>
        new(_fichas.Object, _orders.Object, _sessions.Object, _users.Object, _orderService.Object, _uow.Object);

    [Fact]
    public async Task GetFichasAsync_Instructor_SeesOnlyOwnFichas()
    {
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new Ficha { Id = 1, FichaCode = "F1", ProcessName = "Trazo", InstructorName = "Laura Gómez", InstructorUserId = 10 },
            new Ficha { Id = 2, FichaCode = "F2", ProcessName = "Corte", InstructorName = "Carlos Méndez", InstructorUserId = 20 }
        ]);

        var result = await CreateSut().GetFichasAsync(10, UserRoles.Instructor, "Laura Gómez");

        Assert.Single(result);
        Assert.Equal("F1", result[0].FichaCode);
    }

    [Fact]
    public async Task GetFichasAsync_Administrator_SeesAllFichas()
    {
        _fichas.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new Ficha { Id = 1, FichaCode = "F1", InstructorName = "Laura Gómez", InstructorUserId = 10 },
            new Ficha { Id = 2, FichaCode = "F2", InstructorName = "Carlos Méndez", InstructorUserId = 20 }
        ]);

        var result = await CreateSut().GetFichasAsync(1, UserRoles.Administrador, "Admin");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRecentSessionsAsync_Instructor_SeesOnlyOwnSessions()
    {
        _sessions.Setup(r => r.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ProductionSession
            {
                Id = 1,
                Units = 5,
                SessionDate = DateTime.Now,
                RegisteredByUserId = 10,
                Ficha = new Ficha { FichaCode = "F1", InstructorName = "Laura Gómez", InstructorUserId = 10 },
                ProductionOrder = new ProductionOrder { OrderNumber = "OP-001" }
            },
            new ProductionSession
            {
                Id = 2,
                Units = 8,
                SessionDate = DateTime.Now,
                RegisteredByUserId = 20,
                Ficha = new Ficha { FichaCode = "F2", InstructorName = "Carlos Méndez", InstructorUserId = 20 },
                ProductionOrder = new ProductionOrder { OrderNumber = "OP-002" }
            }
        ]);

        var result = await CreateSut().GetRecentSessionsAsync(10, UserRoles.Instructor, "Laura Gómez");

        Assert.Single(result);
        Assert.Equal("F1", result[0].FichaCode);
        Assert.Equal(5, result[0].Units);
    }

    [Fact]
    public async Task RegisterSessionAsync_Instructor_CannotUseOtherInstructorFicha()
    {
        _fichas.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(
            new Ficha { Id = 2, FichaCode = "F2", InstructorName = "Carlos", InstructorUserId = 20 });

        var result = await CreateSut().RegisterSessionAsync(
            new RegisterProductionDto(1, 2, 5, "test"),
            registeredByUserId: 10,
            viewerRole: UserRoles.Instructor,
            viewerName: "Laura Gómez");

        Assert.False(result.Success);
        Assert.Contains("propias fichas", result.Message);
        _sessions.Verify(r => r.AddAsync(It.IsAny<ProductionSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
