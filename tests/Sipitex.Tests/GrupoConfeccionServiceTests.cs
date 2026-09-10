using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class GrupoConfeccionServiceTests
{
    private readonly Mock<IGrupoConfeccionRepository> _grupos = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private GrupoConfeccionService CreateSut() => new(
        _grupos.Object, _orders.Object, _users.Object, _consumos.Object, _uow.Object);

    [Fact]
    public async Task CreateAsync_InstructorYOrden_Ok()
    {
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 4, OrderNumber = "OP-4" });
        _users.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 9, Nombre = "Laura", Rol = UserRoles.Instructor });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().CreateAsync(new CreateGrupoConfeccionDto(
            4, 9, DateOnly.FromDateTime(DateTime.Today), new TimeOnly(8, 0), new TimeOnly(12, 0), 20));

        Assert.True(result.Success, result.Message);
        _grupos.Verify(r => r.AddAsync(It.Is<GrupoConfeccion>(g =>
            g.ProductionOrderId == 4 && g.InstructorUserId == 9 && g.CantidadPrendas == 20),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetConsumosCruzadosAsync_GrupoToOrderToMaterials()
    {
        var grupo = new GrupoConfeccion
        {
            Id = 2,
            ProductionOrderId = 4,
            ProductionOrder = new ProductionOrder { Id = 4, OrderNumber = "OP-4" },
            InstructorUserId = 9,
            Instructor = new User { Id = 9, Nombre = "Laura" },
            FechaRealizacion = DateOnly.FromDateTime(DateTime.Today),
            HoraInicio = new TimeOnly(8, 0)
        };
        _grupos.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(grupo);
        _consumos.Setup(r => r.GetByOrderIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial
                {
                    Id = 1,
                    ProductionOrderId = 4,
                    ProductionOrder = grupo.ProductionOrder,
                    MaterialId = 3,
                    Material = new Material { Id = 3, Code = "mat3", Name = "Hilo", Unit = MaterialUnit.Metros },
                    Cantidad = 5,
                    CostoUnitario = 2,
                    ResponsableUserId = 9,
                    Responsable = new User { Id = 9, Nombre = "Laura" }
                }
            ]);

        var cruzado = await CreateSut().GetConsumosCruzadosAsync(2);

        Assert.NotNull(cruzado);
        Assert.Equal("OP-4", cruzado!.OrderNumber);
        var line = Assert.Single(cruzado.Consumos);
        Assert.Equal("mat3", line.MaterialCode);
        Assert.Equal(10, line.CostoTotal);
    }
}
