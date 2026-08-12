using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

/// <summary>
/// Fase B: piezas de patronaje y tabla de medidas por talla.
/// </summary>
public class BomProductPatronajePhaseBTests
{
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private BomCatalogService CreateSut() =>
        new(_boms.Object, _materials.Object, _users.Object, _orders.Object, _uow.Object);

    private static Material Mat(int id = 1) => new()
    {
        Id = id,
        Code = $"mat{id}",
        Name = "Tela Jersey",
        Unit = MaterialUnit.Metros,
        Stock = 100
    };

    private static BomRecipeLineDto Line() =>
        new(null, 1, null, null, 0.5m, MaterialUnit.Metros);

    [Fact]
    public async Task CreateAsync_WithPiezasAndMedidasPorTalla_PersistsGraph()
    {
        _boms.Setup(r => r.GetProductByNameAsync("Blusa", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProduct?)null);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat());

        BomProduct? saved = null;
        _boms.Setup(r => r.AddProductAsync(It.IsAny<BomProduct>(), It.IsAny<CancellationToken>()))
            .Callback<BomProduct, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(new UpsertBomProductDto(
            "Blusa",
            false,
            null,
            true,
            [Line()],
            Tallas:
            [
                new BomProductTallaDto(null, "S/36", 0),
                new BomProductTallaDto(null, "M/38", 1)
            ],
            Piezas:
            [
                new BomProductPiezaDto(null, "Delantero", 2, "PAÑO", 0),
                new BomProductPiezaDto(null, "Bolsillo", 1, "LONA", 1)
            ],
            Medidas:
            [
                new BomProductMedidaDto(
                    null,
                    BomMedidaTipo.Patron,
                    "A",
                    "Largo hombro",
                    "+/-0.5",
                    "Desde cuello hasta hombro",
                    0,
                    [
                        new BomProductMedidaValorDto(null, 0, "S/36", 8.5m),
                        new BomProductMedidaValorDto(null, 1, "M/38", 9.0m)
                    ]),
                new BomProductMedidaDto(
                    null,
                    BomMedidaTipo.PrendaTerminada,
                    "A",
                    "Largo hombro",
                    "0",
                    null,
                    0,
                    [
                        new BomProductMedidaValorDto(null, 0, "S/36", 8.0m),
                        new BomProductMedidaValorDto(null, 1, "M/38", 8.5m)
                    ])
            ]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.Tallas.Count);
        Assert.Equal(2, saved.Piezas.Count);
        Assert.Equal("Delantero", saved.Piezas.OrderBy(p => p.Orden).First().Nombre);
        Assert.Equal(2, saved.Medidas.Count);
        var patron = Assert.Single(saved.Medidas, m => m.Tipo == BomMedidaTipo.Patron);
        Assert.Equal("A", patron.Codigo);
        Assert.Equal("+/-0.5", patron.Tolerancia);
        Assert.Equal(2, patron.Valores.Count);
        Assert.Contains(patron.Valores, v => v.Talla.Nombre == "S/36" && v.Valor == 8.5m);
        Assert.Contains(patron.Valores, v => v.Talla.Nombre == "M/38" && v.Valor == 9.0m);
        Assert.Contains(saved.Medidas, m => m.Tipo == BomMedidaTipo.PrendaTerminada);
    }

    [Fact]
    public async Task CreateAsync_MedidasWithoutTallas_ReturnsClearError()
    {
        _boms.Setup(r => r.GetProductByNameAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProduct?)null);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat());

        var result = await CreateSut().CreateAsync(new UpsertBomProductDto(
            "Camisa",
            false,
            null,
            true,
            [Line()],
            Tallas: null,
            Medidas:
            [
                new BomProductMedidaDto(
                    null,
                    BomMedidaTipo.Patron,
                    "A",
                    "Largo hombro",
                    null,
                    null,
                    0,
                    [new BomProductMedidaValorDto(null, 0, null, 10m)])
            ]), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("talla", result.Message, StringComparison.OrdinalIgnoreCase);
        _boms.Verify(r => r.AddProductAsync(It.IsAny<BomProduct>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RemovingTalla_ClearsOldMedidasAndAppliesCascadePolicy()
    {
        // Política documentada: al reemplazar/eliminar tallas se eliminan valores
        // asociados (cascade) y se reescriben medidas del DTO — sin huérfanos silenciosos.
        var oldTalla = new BomProductTalla { Id = 1, BomProductId = 5, Nombre = "UNICA", Orden = 0 };
        var oldMedida = new BomProductMedida
        {
            Id = 10,
            BomProductId = 5,
            Tipo = BomMedidaTipo.Patron,
            Codigo = "A",
            Descripcion = "Largo",
            Orden = 0,
            Valores =
            [
                new BomProductMedidaValor
                {
                    Id = 100,
                    BomProductMedidaId = 10,
                    BomProductTallaId = 1,
                    Talla = oldTalla,
                    Valor = 12m
                }
            ]
        };
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
                    Material = Mat(),
                    QuantityPerUnit = 0.8m,
                    Unit = MaterialUnit.Metros
                }
            ],
            Tallas = [oldTalla],
            Medidas = [oldMedida]
        };

        _boms.Setup(r => r.GetProductByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _boms.Setup(r => r.GetProductByNameAsync("Camiseta", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat());

        var result = await CreateSut().UpdateAsync(5, new UpsertBomProductDto(
            "Camiseta",
            false,
            null,
            true,
            [new BomRecipeLineDto(10, 1, null, null, 0.8m, MaterialUnit.Metros)],
            Tallas:
            [
                new BomProductTallaDto(null, "S", 0),
                new BomProductTallaDto(null, "M", 1)
            ],
            Medidas:
            [
                new BomProductMedidaDto(
                    null,
                    BomMedidaTipo.Patron,
                    "A",
                    "Largo",
                    null,
                    null,
                    0,
                    [
                        new BomProductMedidaValorDto(null, 0, "S", 10m),
                        new BomProductMedidaValorDto(null, 1, "M", 11m)
                    ])
            ]), CancellationToken.None);

        Assert.True(result.Success);
        _boms.Verify(r => r.RemoveMedida(It.IsAny<BomProductMedida>()), Times.AtLeastOnce);
        _boms.Verify(r => r.RemoveTalla(It.IsAny<BomProductTalla>()), Times.AtLeastOnce);
        Assert.Equal(2, product.Tallas.Count);
        Assert.DoesNotContain(product.Tallas, t => t.Nombre == "UNICA");
        Assert.Single(product.Medidas);
        Assert.Equal(2, product.Medidas.First().Valores.Count);
    }

    [Fact]
    public async Task CreateAsync_PiezasOnlyWithoutMedidas_SucceedsWithoutTallas()
    {
        _boms.Setup(r => r.GetProductByNameAsync("Overol", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BomProduct?)null);
        _materials.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Mat());

        BomProduct? saved = null;
        _boms.Setup(r => r.AddProductAsync(It.IsAny<BomProduct>(), It.IsAny<CancellationToken>()))
            .Callback<BomProduct, CancellationToken>((p, _) => saved = p)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().CreateAsync(new UpsertBomProductDto(
            "Overol",
            false,
            null,
            true,
            [Line()],
            Piezas: [new BomProductPiezaDto(null, "Espalda", 1, "DRIL", 0)]), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Single(saved!.Piezas);
        Assert.Empty(saved.Medidas);
        Assert.Empty(saved.Tallas);
    }
}
