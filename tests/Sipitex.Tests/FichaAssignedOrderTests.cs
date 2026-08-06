using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class FichaAssignedOrderTests
{
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionSessionRepository> _sessions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProductionOrderService> _orderService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private FichaService CreateSut() =>
        new(_fichas.Object, _orders.Object, _sessions.Object, _users.Object, _orderService.Object, _uow.Object);

    public FichaAssignedOrderTests()
    {
        _fichas.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _users.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = 10,
            Nombre = "Laura Gómez",
            Rol = UserRoles.Instructor,
            IsActive = true,
            PasswordHash = "x"
        });
    }

    [Fact]
    public async Task CreateFicha_OnlyOrderId_PersistsFkAndNullText()
    {
        _orders.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 7, OrderNumber = "OP-007" });

        Ficha? saved = null;
        _fichas.Setup(r => r.AddAsync(It.IsAny<Ficha>(), It.IsAny<CancellationToken>()))
            .Callback<Ficha, CancellationToken>((f, _) => saved = f)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateFichaAsync(
            new CreateFichaDto("FICHA-A1", "Corte", [10], "Mañana", 7, null));

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal(7, saved!.ProductionOrderId);
        Assert.Null(saved.AssignedOrderText);
    }

    [Fact]
    public async Task CreateFicha_OnlyAssignedOrderText_PersistsTextAndNullFk()
    {
        Ficha? saved = null;
        _fichas.Setup(r => r.AddAsync(It.IsAny<Ficha>(), It.IsAny<CancellationToken>()))
            .Callback<Ficha, CancellationToken>((f, _) => saved = f)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateFichaAsync(
            new CreateFichaDto("FICHA-A2", "Trazo", [10], "Tarde", null, "  OP-EXT-01  "));

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Null(saved!.ProductionOrderId);
        Assert.Equal("OP-EXT-01", saved.AssignedOrderText);
        _orders.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFicha_BothOrderIdAndText_FailsValidation()
    {
        var result = await CreateSut().CreateFichaAsync(
            new CreateFichaDto("FICHA-A3", "Corte", [10], "Mañana", 7, "OP-MANUAL"));

        Assert.False(result.Success);
        Assert.Equal(
            "No puedes seleccionar una orden y escribir una manual al mismo tiempo",
            result.Message);
        _fichas.Verify(r => r.AddAsync(It.IsAny<Ficha>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFicha_NeitherOrder_SucceedsWithBothNull()
    {
        Ficha? saved = null;
        _fichas.Setup(r => r.AddAsync(It.IsAny<Ficha>(), It.IsAny<CancellationToken>()))
            .Callback<Ficha, CancellationToken>((f, _) => saved = f)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateFichaAsync(
            new CreateFichaDto("FICHA-A4", "Calidad", [10], "Noche", null, "   "));

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Null(saved!.ProductionOrderId);
        Assert.Null(saved.AssignedOrderText);
    }
}
