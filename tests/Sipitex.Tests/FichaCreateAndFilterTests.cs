using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Tests;

public class FichaCreateAndFilterTests
{
    private readonly Mock<IFichaRepository> _fichas = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionSessionRepository> _sessions = new();
    private readonly Mock<IProductionOrderService> _orderService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private FichaService CreateSut() =>
        new(_fichas.Object, _orders.Object, _sessions.Object, _orderService.Object, _uow.Object);

    [Fact]
    public async Task CreateFichaAsync_WhenCodeIsNew_Succeeds()
    {
        _fichas.Setup(r => r.ExistsByCodeAsync("FICHA-N1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateSut().CreateFichaAsync(
            new CreateFichaDto("FICHA-N1", "Corte", "Laura Gómez", "Mañana", null));

        Assert.True(result.Success);
        Assert.Contains("FICHA-N1", result.Message);
        _fichas.Verify(r => r.AddAsync(
            It.Is<Ficha>(f => f.FichaCode == "FICHA-N1" && f.Turno == "Mañana" && f.ProcessName == "Corte"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFichaAsync_WhenCodeExists_FailsWithClearMessage()
    {
        _fichas.Setup(r => r.ExistsByCodeAsync("FICHA-T1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateSut().CreateFichaAsync(
            new CreateFichaDto("FICHA-T1", "Trazo", "Laura Gómez", "Mañana"));

        Assert.False(result.Success);
        Assert.Equal("Ya existe una ficha con ese código.", result.Message);
        _fichas.Verify(r => r.AddAsync(It.IsAny<Ficha>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRecentSessionsAsync_FilterByInstructorAndTurno_ReturnsOnlyExpected()
    {
        _sessions.Setup(r => r.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ProductionSession
            {
                Id = 1,
                Units = 5,
                SessionDate = DateTime.Now,
                Ficha = new Ficha { FichaCode = "F1", InstructorName = "Laura Gómez", Turno = "Mañana" },
                ProductionOrder = new ProductionOrder { OrderNumber = "OP-001" }
            },
            new ProductionSession
            {
                Id = 2,
                Units = 8,
                SessionDate = DateTime.Now,
                Ficha = new Ficha { FichaCode = "F2", InstructorName = "Carlos Méndez", Turno = "Tarde" },
                ProductionOrder = new ProductionOrder { OrderNumber = "OP-002" }
            },
            new ProductionSession
            {
                Id = 3,
                Units = 3,
                SessionDate = DateTime.Now,
                Ficha = new Ficha { FichaCode = "F3", InstructorName = "Laura Gómez", Turno = "Tarde" },
                ProductionOrder = new ProductionOrder { OrderNumber = "OP-003" }
            }
        ]);

        var all = await CreateSut().GetRecentSessionsAsync(1, UserRoles.Administrador, "Admin");

        var filtered = all
            .Where(s => s.InstructorName.Contains("Laura", StringComparison.OrdinalIgnoreCase))
            .Where(s => string.Equals(s.Turno, "Mañana", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Single(filtered);
        Assert.Equal("F1", filtered[0].FichaCode);
        Assert.Equal("Mañana", filtered[0].Turno);
        Assert.Equal("Laura Gómez", filtered[0].InstructorName);
    }
}
