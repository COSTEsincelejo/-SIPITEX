using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class BomCatalogServiceTests
{
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private BomCatalogService CreateSut() => new(_boms.Object, _materials.Object, _users.Object, _uow.Object);

    private static Material Mat(int id, string name, MaterialUnit unit = MaterialUnit.Metros) => new()
    {
        Id = id,
        Code = $"mat{id}",
        Name = name,
        Unit = unit,
        Stock = 100
    };

    [Fact]
    public async Task CreateAsync_WithValidLines_Succeeds()
    {
        _boms.Setup(r => r.GetProductByNameAsync("Camiseta", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProduct?)null);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mat(1, "Tela Jersey"));

        BomProduct? saved = null;
        _boms.Setup(r => r.AddProductAsync(It.IsAny<BomProduct>(), It.IsAny<CancellationToken>()))
            .Callback<BomProduct, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(new UpsertBomProductDto(
            "Camiseta",
            IsReference: true,
            Notes: null,
            HabilitadoParaOrdenes: true,
            Lines:
            [
                new BomRecipeLineDto(null, 1, null, null, 0.8m, MaterialUnit.Metros)
            ]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("Camiseta", saved!.ProductName);
        Assert.True(saved.IsReference);
        Assert.Contains("referencia", saved.Notes, StringComparison.OrdinalIgnoreCase);
        Assert.Single(saved.Items);
        Assert.Equal(0.8m, saved.Items.First().QuantityPerUnit);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateAsync_ChangeQuantity_Succeeds()
    {
        var product = new BomProduct
        {
            Id = 5,
            ProductName = "Camiseta",
            HabilitadoParaOrdenes = true,
            Items =
            [
                new BomItem
                {
                    Id = 10,
                    BomProductId = 5,
                    ProductName = "Camiseta",
                    MaterialId = 1,
                    Material = Mat(1, "Tela Jersey"),
                    QuantityPerUnit = 0.8m,
                    Unit = MaterialUnit.Metros
                }
            ]
        };

        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _boms.Setup(r => r.GetProductByNameAsync("Camiseta", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat(1, "Tela Jersey"));

        var result = await CreateSut().UpdateAsync(5, new UpsertBomProductDto(
            "Camiseta",
            false,
            null,
            true,
            [new BomRecipeLineDto(10, 1, null, null, 1.1m, MaterialUnit.Metros)]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(product.Items);
        Assert.Equal(1.1m, product.Items.First().QuantityPerUnit);
        _boms.Verify(r => r.RemoveItem(It.IsAny<BomItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AddMaterialToExistingRecipe_Succeeds()
    {
        var product = new BomProduct
        {
            Id = 5,
            ProductName = "Overol",
            HabilitadoParaOrdenes = true,
            Items =
            [
                new BomItem
                {
                    Id = 10,
                    BomProductId = 5,
                    ProductName = "Overol",
                    MaterialId = 2,
                    Material = Mat(2, "Tela Dril"),
                    QuantityPerUnit = 2.3m,
                    Unit = MaterialUnit.Metros
                }
            ]
        };

        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _boms.Setup(r => r.GetProductByNameAsync("Overol", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _materials.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(Mat(2, "Tela Dril"));
        _materials.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mat(3, "Cremallera invisible", MaterialUnit.Unidades));

        var result = await CreateSut().UpdateAsync(5, new UpsertBomProductDto(
            "Overol",
            true,
            "ref",
            true,
            [
                new BomRecipeLineDto(10, 2, null, null, 2.3m, MaterialUnit.Metros),
                new BomRecipeLineDto(null, 3, null, null, 1m, MaterialUnit.Unidades)
            ]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, product.Items.Count);
    }

    [Fact]
    public async Task UpdateAsync_RemoveMaterialFromRecipe_Succeeds()
    {
        var product = new BomProduct
        {
            Id = 5,
            ProductName = "Pantaloneta",
            HabilitadoParaOrdenes = true,
            Items =
            [
                new BomItem { Id = 1, BomProductId = 5, ProductName = "Pantaloneta", MaterialId = 1, Material = Mat(1, "Jersey"), QuantityPerUnit = 1m, Unit = MaterialUnit.Metros },
                new BomItem { Id = 2, BomProductId = 5, ProductName = "Pantaloneta", MaterialId = 2, Material = Mat(2, "Hilo"), QuantityPerUnit = 8m, Unit = MaterialUnit.Metros }
            ]
        };

        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _boms.Setup(r => r.GetProductByNameAsync("Pantaloneta", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat(1, "Jersey"));

        var result = await CreateSut().UpdateAsync(5, new UpsertBomProductDto(
            "Pantaloneta",
            true,
            null,
            true,
            [new BomRecipeLineDto(1, 1, null, null, 1m, MaterialUnit.Metros)]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(product.Items);
        Assert.Equal(1, product.Items.First().MaterialId);
        _boms.Verify(r => r.RemoveItem(It.IsAny<BomItem>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_RemovesProduct_Succeeds()
    {
        var product = new BomProduct { Id = 9, ProductName = "Camiseta", Items = [] };
        _boms.Setup(r => r.GetProductByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await CreateSut().DeleteAsync(9, CancellationToken.None);

        Assert.True(result.Success);
        _boms.Verify(r => r.RemoveProduct(product), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
