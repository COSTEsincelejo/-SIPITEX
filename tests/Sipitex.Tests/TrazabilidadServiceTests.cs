using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class TrazabilidadServiceTests
{
    private readonly Mock<IPrendaTrazableRepository> _prendas = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snaps = new();
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IQualityRepository> _quality = new();
    private readonly Mock<IProductionFlowRepository> _flow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IActivityLogService> _log = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private TrazabilidadService CreateSut() => new(
        _prendas.Object, _orders.Object, _snaps.Object, _consumos.Object,
        _quality.Object, _flow.Object, _users.Object, _log.Object, _uow.Object);

    private static TrazabilidadViewerFilter Admin() =>
        new(UserRoles.Administrador, 1, []);

    [Fact]
    public void PrefixFor_UsaAnioMesYIdDeOrden()
    {
        var utc = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal("SIPITEX-202609-4-", TrazabilidadService.PrefixFor(4, utc));
        Assert.Equal("SIPITEX-202609-9-", TrazabilidadService.PrefixFor(9, utc));
        Assert.NotEqual(
            TrazabilidadService.PrefixFor(1, utc),
            TrazabilidadService.PrefixFor(2, utc));
    }

    [Fact]
    public async Task GenerarAsync_CreaCodigosConsecutivos()
    {
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder
            {
                Id = 4,
                OrderNumber = "OP-001",
                ProductName = "Polo",
                TotalQuantity = 10,
                ProducedQuantity = 3,
                EstadoProducto = EstadoProducto.Confeccion
            });
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _prendas.Setup(r => r.CountByOrderAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _prendas.Setup(r => r.GetLastCodigoForPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        IReadOnlyList<PrendaTrazable>? saved = null;
        _prendas.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<PrendaTrazable>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<PrendaTrazable>, CancellationToken>((items, _) => saved = items)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().GenerarAsync(Admin(), 4, 2, 7);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(saved);
        var prefix = TrazabilidadService.PrefixFor(4, DateTime.UtcNow);
        Assert.Equal(new[] { prefix + "0001", prefix + "0002" }, saved!.Select(p => p.Codigo));
        Assert.All(saved, p => Assert.Equal("Polo", p.ProductName));
    }

    [Fact]
    public async Task GenerarAsync_OrdenesDistintasMismoDia_NoDuplicanCodigos()
    {
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _prendas.Setup(r => r.GetLastCodigoForPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        foreach (var orderId in new[] { 4, 9 })
        {
            _orders.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductionOrder
                {
                    Id = orderId,
                    OrderNumber = $"OP-{orderId:000}",
                    ProductName = "Polo",
                    TotalQuantity = 5
                });
            _prendas.Setup(r => r.CountByOrderAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        }

        var saved = new List<PrendaTrazable>();
        _prendas.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyList<PrendaTrazable>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<PrendaTrazable>, CancellationToken>((items, _) => saved.AddRange(items))
            .Returns(Task.CompletedTask);

        var a = await CreateSut().GenerarAsync(Admin(), 4, 1, 7);
        var b = await CreateSut().GenerarAsync(Admin(), 9, 1, 7);

        Assert.True(a.Success, a.Message);
        Assert.True(b.Success, b.Message);
        Assert.Equal(2, saved.Count);
        Assert.Equal(2, saved.Select(p => p.Codigo).Distinct().Count());
        var utc = DateTime.UtcNow;
        Assert.Equal(TrazabilidadService.PrefixFor(4, utc) + "0001", saved[0].Codigo);
        Assert.Equal(TrazabilidadService.PrefixFor(9, utc) + "0001", saved[1].Codigo);
    }

    [Fact]
    public async Task GenerarAsync_NoExcedeCupoDeLaOrden()
    {
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder
            {
                Id = 4,
                OrderNumber = "OP-9",
                ProductName = "Cofia",
                TotalQuantity = 2,
                ProducedQuantity = 2
            });
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _prendas.Setup(r => r.CountByOrderAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateSut().GenerarAsync(Admin(), 4, 5, 7);

        Assert.False(result.Success);
        Assert.Contains("ya tiene", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetByCodigoAsync_InstructorSinAlcance_Nulo()
    {
        _prendas.Setup(r => r.GetByCodigoAsync("SIPITEX-202609-99-0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrendaTrazable
            {
                Id = 1,
                Codigo = "SIPITEX-202609-99-0001",
                ProductionOrderId = 99,
                ProductName = "Polo",
                CreadoPor = new User { Id = 2, Nombre = "Otro" }
            });

        var filter = new TrazabilidadViewerFilter(UserRoles.Instructor, 10, AllowedOrderIds: [1, 2]);
        var detail = await CreateSut().GetByCodigoAsync(filter, "SIPITEX-202609-99-0001");

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetByCodigoAsync_MuestraSnapshotCostoYBodega()
    {
        _prendas.Setup(r => r.GetByCodigoAsync("SIPITEX-202609-4-0001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrendaTrazable
            {
                Id = 1,
                Codigo = "SIPITEX-202609-4-0001",
                ProductionOrderId = 4,
                ProductName = "Polo",
                CreadoUtc = new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc),
                CreadoPor = new User { Id = 7, Nombre = "Ana" },
                ProductionOrder = new ProductionOrder
                {
                    Id = 4,
                    OrderNumber = "OP-001",
                    ProductName = "Polo",
                    TotalQuantity = 10
                }
            });
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 4, OrderNumber = "OP-001", ProductName = "Polo", TotalQuantity = 10 });
        _snaps.Setup(r => r.GetByOrderIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProductionOrderBomSnapshot { MaterialName = "Jersey", QuantityPerUnit = 1.6m }
            ]);
        _consumos.Setup(r => r.GetByOrderIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial
                {
                    MaterialId = 3,
                    Cantidad = 20,
                    CostoUnitarioAlMomento = 5,
                    FechaUtc = new DateTime(2026, 9, 16, 8, 0, 0, DateTimeKind.Utc),
                    Material = new Material
                    {
                        Id = 3,
                        Name = "Jersey",
                        Unit = MaterialUnit.Metros,
                        CostoPromedioPonderado = 99,
                        PlantaInventarioId = 1,
                        PlantaInventario = new PlantaInventario { Id = 1, Nombre = "Planta 1" }
                    }
                }
            ]);
        _quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _flow.Setup(r => r.GetHistoryByOrderAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var detail = await CreateSut().GetByCodigoAsync(Admin(), "SIPITEX-202609-4-0001");

        Assert.NotNull(detail);
        Assert.Equal("Polo", detail!.ProductName);
        var insumo = Assert.Single(detail.InsumosConsumidos);
        Assert.Equal(5, insumo.CostoUnitarioAlMomento);
        Assert.Equal(100, insumo.CostoLinea);
        Assert.Equal("Planta 1", insumo.BodegaOrigen);
        Assert.Equal(10, detail.CostoMaterialesPorUnidad);
        Assert.Equal(100, detail.CostoFinalOrden);
        Assert.NotEqual(99, insumo.CostoUnitarioAlMomento);
    }
}
