using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class InventoryBodegaScopeTests
{
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IMaterialRequestRepository> _requests = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<IBodegaRepository> _bodegas = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private InventoryService CreateSut()
    {
        _bodegas
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bodega { Id = 1, Nombre = "Bodega 1" });
        _bodegas
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Bodega { Id = 2, Nombre = "Bodega 2" });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new(
            _materials.Object,
            _requests.Object,
            _orders.Object,
            _boms.Object,
            _stockMovements.Object,
            _bodegas.Object,
            _uow.Object);
    }

    private static Material MaterialDe(int id, string name, int bodegaId, string bodegaNombre) => new()
    {
        Id = id,
        Name = name,
        Unit = MaterialUnit.Metros,
        Stock = 10,
        Status = MaterialStatus.Bueno,
        MinStock = 1,
        LastEntryDate = new DateOnly(2026, 1, 1),
        BodegaId = bodegaId,
        Bodega = new Bodega { Id = bodegaId, Nombre = bodegaNombre }
    };

    [Fact]
    public async Task AddMaterialAsync_Bodeguero_WhenBodegaIdDoesNotMatch_Fails()
    {
        var result = await CreateSut().AddMaterialAsync(
            new CreateMaterialDto("Hilo", 5m, MaterialUnit.Metros, StockEntryOrigin.Compra, BodegaId: 2),
            actorUserId: 8,
            actorRole: UserRoles.Bodeguero,
            actorBodegaId: 1);

        Assert.False(result.Success);
        Assert.Contains("propia bodega", result.Message, StringComparison.OrdinalIgnoreCase);
        _materials.Verify(
            r => r.AddAsync(It.IsAny<Material>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddMaterialAsync_Bodeguero_WhenActorBodegaIdIsNull_Fails()
    {
        var result = await CreateSut().AddMaterialAsync(
            new CreateMaterialDto("Hilo", 5m, MaterialUnit.Metros, StockEntryOrigin.Compra, BodegaId: 1),
            actorUserId: 8,
            actorRole: UserRoles.Bodeguero,
            actorBodegaId: null);

        Assert.False(result.Success);
        Assert.Contains("bodega asignada", result.Message, StringComparison.OrdinalIgnoreCase);
        _materials.Verify(
            r => r.AddAsync(It.IsAny<Material>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddMaterialAsync_Administrador_AssignsDtoBodegaId()
    {
        Material? saved = null;
        _materials
            .Setup(r => r.AddAsync(It.IsAny<Material>(), It.IsAny<CancellationToken>()))
            .Callback<Material, CancellationToken>((m, _) =>
            {
                m.Id = 40;
                saved = m;
            })
            .Returns(Task.CompletedTask);
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().AddMaterialAsync(
            new CreateMaterialDto("Botón", 8m, MaterialUnit.Unidades, StockEntryOrigin.Compra, BodegaId: 2),
            actorUserId: 1,
            actorRole: UserRoles.Administrador,
            actorBodegaId: null);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.BodegaId);
    }

    [Fact]
    public async Task GetMaterialsAsync_FiltersByBodegaId_AndNullReturnsAll()
    {
        _materials.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            MaterialDe(1, "Tela", 1, "Bodega 1"),
            MaterialDe(2, "Hilo", 2, "Bodega 2")
        ]);

        var sut = CreateSut();
        var bodega1 = await sut.GetMaterialsAsync(bodegaId: 1);
        var all = await sut.GetMaterialsAsync(bodegaId: null);

        Assert.Single(bodega1);
        Assert.Equal("Tela", bodega1[0].Name);
        Assert.Equal(1, bodega1[0].BodegaId);
        Assert.Equal("Bodega 1", bodega1[0].BodegaNombre);
        Assert.NotEqual("—", bodega1[0].BodegaNombre);

        Assert.Equal(2, all.Count);
        Assert.Contains(all, m => m.BodegaNombre == "Bodega 1");
        Assert.Contains(all, m => m.BodegaNombre == "Bodega 2");
    }
}
