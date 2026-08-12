using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

/// <summary>
/// Fase A: metadatos base y tallas de ficha técnica (BomProduct / BomProductTalla).
/// </summary>
public class BomProductMetadataPhaseATests
{
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private BomCatalogService CreateSut() => new(_boms.Object, _materials.Object, _users.Object, _uow.Object);

    private static Material Mat(int id = 1) => new()
    {
        Id = id,
        Code = $"mat{id}",
        Name = "Tela Jersey",
        Unit = MaterialUnit.Metros,
        Stock = 100
    };

    private static BomRecipeLineDto Line(int materialId = 1) =>
        new(null, materialId, null, null, 0.5m, MaterialUnit.Metros);

    [Fact]
    public async Task CreateAsync_WithMetadataAndTallas_PersistsAllFields()
    {
        _boms.Setup(r => r.GetProductByNameAsync("Blusa CMTC", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProduct?)null);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat());

        BomProduct? saved = null;
        _boms.Setup(r => r.AddProductAsync(It.IsAny<BomProduct>(), It.IsAny<CancellationToken>()))
            .Callback<BomProduct, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(new UpsertBomProductDto(
            "Blusa CMTC",
            IsReference: false,
            Notes: "Ficha completa",
            HabilitadoParaOrdenes: true,
            Lines: [Line()],
            Referencia: "C-01-001-020",
            Linea: "Ejercicio",
            TallaInicial: "UNICA",
            TipoEmpaque: "Doblado",
            DescripcionPrenda: "Blusa manga corta con cuello redondo",
            FechaSolicitud: new DateOnly(2026, 1, 10),
            FechaElaboracion: new DateOnly(2026, 2, 1),
            AnioMuestrario: 2026,
            EsDisenoNuevo: true,
            EsReplica: false,
            EsBancoDeMuestras: true,
            Disenador: "Ana Pérez",
            Patronista: "Luis Ruiz",
            Digitacion: "Carla Díaz",
            Tallas:
            [
                new BomProductTallaDto(null, "S/36", 0),
                new BomProductTallaDto(null, "M/38", 1),
                new BomProductTallaDto(null, "L/40", 2)
            ]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("C-01-001-020", saved!.Referencia);
        Assert.Equal("Ejercicio", saved.Linea);
        Assert.Equal("UNICA", saved.TallaInicial);
        Assert.Equal("Doblado", saved.TipoEmpaque);
        Assert.Equal("Blusa manga corta con cuello redondo", saved.DescripcionPrenda);
        Assert.Equal(new DateOnly(2026, 1, 10), saved.FechaSolicitud);
        Assert.Equal(new DateOnly(2026, 2, 1), saved.FechaElaboracion);
        Assert.Equal(2026, saved.AnioMuestrario);
        Assert.True(saved.EsDisenoNuevo);
        Assert.False(saved.EsReplica);
        Assert.True(saved.EsBancoDeMuestras);
        Assert.Equal("Ana Pérez", saved.Disenador);
        Assert.Equal("Luis Ruiz", saved.Patronista);
        Assert.Equal("Carla Díaz", saved.Digitacion);
        Assert.Equal(3, saved.Tallas.Count);
        Assert.Equal(["S/36", "M/38", "L/40"], saved.Tallas.OrderBy(t => t.Orden).Select(t => t.Nombre).ToList());
        Assert.Single(saved.Items);
    }

    [Fact]
    public async Task CreateAsync_WithoutMetadata_LeavesOptionalFieldsEmpty()
    {
        // Simula ficha técnica “legacy”: solo nombre + receta (como antes de Fase A)
        _boms.Setup(r => r.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProduct?)null);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat());

        BomProduct? saved = null;
        _boms.Setup(r => r.AddProductAsync(It.IsAny<BomProduct>(), It.IsAny<CancellationToken>()))
            .Callback<BomProduct, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(new UpsertBomProductDto(
            "Camisa",
            IsReference: true,
            Notes: null,
            HabilitadoParaOrdenes: true,
            Lines: [Line()]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Null(saved!.Referencia);
        Assert.Null(saved.Linea);
        Assert.Null(saved.TallaInicial);
        Assert.Null(saved.TipoEmpaque);
        Assert.Null(saved.DescripcionPrenda);
        Assert.Null(saved.FechaSolicitud);
        Assert.Null(saved.FechaElaboracion);
        Assert.Null(saved.AnioMuestrario);
        Assert.False(saved.EsDisenoNuevo);
        Assert.False(saved.EsReplica);
        Assert.False(saved.EsBancoDeMuestras);
        Assert.Null(saved.Disenador);
        Assert.Null(saved.Patronista);
        Assert.Null(saved.Digitacion);
        Assert.Empty(saved.Tallas);
        Assert.Single(saved.Items);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsMetadataAndOrderedTallas()
    {
        var product = new BomProduct
        {
            Id = 9,
            ProductName = "Overol",
            HabilitadoParaOrdenes = true,
            Referencia = "O-02",
            Linea = "Industrial",
            Items =
            [
                new BomItem
                {
                    Id = 1,
                    BomProductId = 9,
                    MaterialId = 1,
                    Material = Mat(),
                    QuantityPerUnit = 2m,
                    Unit = MaterialUnit.Metros,
                    ProductName = "Overol"
                }
            ],
            Tallas =
            [
                new BomProductTalla { Id = 2, BomProductId = 9, Nombre = "L", Orden = 2 },
                new BomProductTalla { Id = 1, BomProductId = 9, Nombre = "S", Orden = 0 },
                new BomProductTalla { Id = 3, BomProductId = 9, Nombre = "M", Orden = 1 }
            ]
        };

        _boms.Setup(r => r.GetProductByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var detail = await CreateSut().GetProductAsync(9);

        Assert.NotNull(detail);
        Assert.Equal("O-02", detail!.Referencia);
        Assert.Equal("Industrial", detail.Linea);
        Assert.Equal(["S", "M", "L"], detail.Tallas!.Select(t => t.Nombre).ToList());
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTallas_WithoutBreakingRecipe()
    {
        var product = new BomProduct
        {
            Id = 5,
            ProductName = "Camiseta",
            HabilitadoParaOrdenes = true,
            Referencia = "OLD",
            Items =
            [
                new BomItem
                {
                    Id = 10,
                    BomProductId = 5,
                    ProductName = "Camiseta",
                    MaterialId = 1,
                    Material = Mat(),
                    QuantityPerUnit = 0.8m,
                    Unit = MaterialUnit.Metros
                }
            ],
            Tallas =
            [
                new BomProductTalla { Id = 1, BomProductId = 5, Nombre = "UNICA", Orden = 0 }
            ]
        };

        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _boms.Setup(r => r.GetProductByNameAsync("Camiseta", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat());

        var result = await CreateSut().UpdateAsync(5, new UpsertBomProductDto(
            "Camiseta",
            false,
            null,
            true,
            [new BomRecipeLineDto(10, 1, null, null, 1.0m, MaterialUnit.Metros)],
            Referencia: "NEW-01",
            Tallas:
            [
                new BomProductTallaDto(null, "8", 0),
                new BomProductTallaDto(null, "10", 1)
            ]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("NEW-01", product.Referencia);
        Assert.Equal(2, product.Tallas.Count);
        Assert.Equal(["8", "10"], product.Tallas.OrderBy(t => t.Orden).Select(t => t.Nombre).ToList());
        Assert.Single(product.Items);
        Assert.Equal(1.0m, product.Items.First().QuantityPerUnit);
        _boms.Verify(r => r.RemoveTalla(It.IsAny<BomProductTalla>()), Times.Once);
    }
}
