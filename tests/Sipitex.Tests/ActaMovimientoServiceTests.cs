using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class ActaMovimientoServiceTests
{
    private readonly Mock<IActaMovimientoRepository> _actas = new();
    private readonly Mock<IStockMovementRepository> _stock = new();
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IActaPdfService> _pdf = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ActaMovimientoService CreateSut() => new(
        _actas.Object, _stock.Object, _consumos.Object, _orders.Object,
        _users.Object, _pdf.Object, _uow.Object);

    private void SeedActorAndCapture()
    {
        _users.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 7, Nombre = "Ana" });
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _actas.Setup(r => r.GetLastNumeroAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _actas.Setup(r => r.AddAsync(It.IsAny<ActaMovimiento>(), It.IsAny<CancellationToken>()))
            .Callback<ActaMovimiento, CancellationToken>((a, _) =>
            {
                a.Id = 1;
                a.CreadoPor = new User { Id = 7, Nombre = "Ana" };
            })
            .Returns(Task.CompletedTask);
        _actas.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
                _actas.Invocations
                    .Select(i => i.Arguments.FirstOrDefault() as ActaMovimiento)
                    .FirstOrDefault(a => a is { Id: 1 }));
    }

    [Fact]
    public async Task CreateAsync_SinNombres_Falla()
    {
        var result = await CreateSut().CreateAsync(new CreateActaDto(
            ActaTipo.Egreso, ActaOrigen.Consumo, "", "Instructor", true, "", "Encargado", true, 7));

        Assert.False(result.Success);
        Assert.Contains("obligatorios", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_DesdeConsumos_CreaEgresoConDetalles()
    {
        SeedActorAndCapture();
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 4, OrderNumber = "OP-4" });
        _consumos.Setup(r => r.GetByOrderIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ConsumoMaterial
                {
                    Id = 11,
                    ProductionOrderId = 4,
                    MaterialId = 3,
                    Material = new Material { Id = 3, Name = "Hilo", Unit = MaterialUnit.Metros },
                    Cantidad = 5
                }
            ]);

        ActaMovimiento? saved = null;
        _actas.Setup(r => r.AddAsync(It.IsAny<ActaMovimiento>(), It.IsAny<CancellationToken>()))
            .Callback<ActaMovimiento, CancellationToken>((a, _) =>
            {
                a.Id = 1;
                a.CreadoPor = new User { Id = 7, Nombre = "Ana" };
                a.ProductionOrder = new ProductionOrder { Id = 4, OrderNumber = "OP-4" };
                saved = a;
            })
            .Returns(Task.CompletedTask);
        _actas.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);

        var result = await CreateSut().CreateAsync(new CreateActaDto(
            ActaTipo.Egreso, ActaOrigen.Consumo,
            "Laura", "Instructor", true,
            "Pedro", "Encargado de bodega", true,
            7, ProductionOrderId: 4));

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("ACT-0001", result.Value!.Numero);
        Assert.Equal(ActaTipo.Egreso, result.Value.Tipo);
        var line = Assert.Single(result.Value.Detalles);
        Assert.Equal("Hilo", line.Descripcion);
        Assert.Equal(5, line.Cantidad);
        Assert.NotNull(result.Value.EntregaConformidadUtc);
        Assert.NotNull(result.Value.RecibeConformidadUtc);
    }

    [Fact]
    public async Task CreateAsync_DesdeStockEntrada_CreaIngreso()
    {
        SeedActorAndCapture();
        ActaMovimiento? saved = null;
        _actas.Setup(r => r.AddAsync(It.IsAny<ActaMovimiento>(), It.IsAny<CancellationToken>()))
            .Callback<ActaMovimiento, CancellationToken>((a, _) =>
            {
                a.Id = 1;
                a.CreadoPor = new User { Id = 7, Nombre = "Ana" };
                saved = a;
            })
            .Returns(Task.CompletedTask);
        _actas.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);
        _stock.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new StockMovement
                {
                    Id = 9,
                    MaterialId = 2,
                    Material = new Material { Id = 2, Name = "Tela", Unit = MaterialUnit.Metros },
                    TipoMovimiento = StockMovementType.Entrada,
                    Cantidad = 12
                }
            ]);

        var result = await CreateSut().CreateAsync(new CreateActaDto(
            ActaTipo.Ingreso, ActaOrigen.Stock,
            "Pedro", "Encargado de bodega", true,
            "Laura", "Instructor", true,
            7, StockMovementIds: [9]));

        Assert.True(result.Success, result.Message);
        Assert.Equal(ActaTipo.Ingreso, result.Value!.Tipo);
        Assert.Equal(9, Assert.Single(result.Value.Detalles).StockMovementId);
    }

    [Fact]
    public async Task CreateAsync_DesdeEstadoProducto_ProductoEnProceso()
    {
        SeedActorAndCapture();
        _orders.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionOrder { Id = 4, OrderNumber = "OP-4" });
        ActaMovimiento? saved = null;
        _actas.Setup(r => r.AddAsync(It.IsAny<ActaMovimiento>(), It.IsAny<CancellationToken>()))
            .Callback<ActaMovimiento, CancellationToken>((a, _) =>
            {
                a.Id = 1;
                a.CreadoPor = new User { Id = 7, Nombre = "Ana" };
                a.ProductionOrder = new ProductionOrder { Id = 4, OrderNumber = "OP-4" };
                saved = a;
            })
            .Returns(Task.CompletedTask);
        _actas.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => saved);

        var result = await CreateSut().CreateAsync(new CreateActaDto(
            ActaTipo.Egreso, ActaOrigen.EstadoProducto,
            "Laura", "Instructor", false,
            "Pedro", "Encargado de bodega", false,
            7, ProductionOrderId: 4,
            EstadoOrigen: EstadoProducto.Corte,
            EstadoDestino: EstadoProducto.Confeccion));

        Assert.True(result.Success, result.Message);
        var line = Assert.Single(result.Value!.Detalles);
        Assert.Equal(ActaItemTipo.ProductoEnProceso, line.ItemTipo);
        Assert.Contains("Corte", line.Descripcion, StringComparison.Ordinal);
        Assert.Null(result.Value.EntregaConformidadUtc);
    }
}
