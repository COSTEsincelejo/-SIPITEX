using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class InventoryDeleteMaterialTests
{
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IMaterialRequestRepository> _requests = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private InventoryService CreateSut() => new(
        _materials.Object,
        _requests.Object,
        _orders.Object,
        _boms.Object,
        _stockMovements.Object,
        _uow.Object);

    [Fact]
    public async Task DeleteMaterialAsync_WhenUsedInBom_BlocksWithProductNames()
    {
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 1, Name = "Tela Jersey", Unit = MaterialUnit.Metros });
        _boms.Setup(r => r.GetProductNamesUsingMaterialAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Camisa", "Camiseta"]);

        var result = await CreateSut().DeleteMaterialAsync(1, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Camisa", result.Message);
        Assert.Contains("Camiseta", result.Message);
        _materials.Verify(r => r.Remove(It.IsAny<Material>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMaterialAsync_WhenNotUsed_Removes()
    {
        var mat = new Material { Id = 4, Name = "Sobrante", Unit = MaterialUnit.Metros };
        _materials.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(mat);
        _boms.Setup(r => r.GetProductNamesUsingMaterialAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateSut().DeleteMaterialAsync(4, CancellationToken.None);

        Assert.True(result.Success);
        _materials.Verify(r => r.Remove(mat), Times.Once);
    }
}
