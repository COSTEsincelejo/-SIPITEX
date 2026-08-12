using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

/// <summary>
/// Gap #15 (AUDITORIA_ROLES_FUNCIONES): cada punto que modifica stock registra exactamente un StockMovement.
/// </summary>
public class StockMovementLedgerTests
{
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IMaterialRequestRepository> _requests = new();
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private InventoryService CreateInventorySut() => new(
        _materials.Object,
        _requests.Object,
        _orders.Object,
        _boms.Object,
        _stockMovements.Object,
        _uow.Object);

    [Fact]
    public async Task AddMaterialAsync_RecordsExactlyOneEntradaMovement()
    {
        StockMovement? captured = null;
        _materials
            .Setup(r => r.AddAsync(It.IsAny<Material>(), It.IsAny<CancellationToken>()))
            .Callback<Material, CancellationToken>((m, _) => m.Id = 11)
            .Returns(Task.CompletedTask);
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateInventorySut().AddMaterialAsync(
            new CreateMaterialDto("Tela Jersey", 25m, MaterialUnit.Metros),
            actorUserId: 7);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(11, captured!.MaterialId);
        Assert.Equal(7, captured.UsuarioId);
        Assert.Equal(StockMovementType.Entrada, captured.TipoMovimiento);
        Assert.Equal(25m, captured.Cantidad);
        Assert.Equal(25m, captured.StockResultante);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AdjustStockAsync_RecordsExactlyOneAjusteMovement()
    {
        var material = new Material
        {
            Id = 4,
            Name = "Hilo",
            Stock = 10m,
            Unit = MaterialUnit.Metros
        };
        _materials.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        StockMovement? captured = null;
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var result = await CreateInventorySut().AdjustStockAsync(
            new AdjustStockDto(4, 18m),
            actorUserId: 3);

        Assert.True(result.Success);
        Assert.Equal(18m, material.Stock);
        Assert.NotNull(captured);
        Assert.Equal(4, captured!.MaterialId);
        Assert.Equal(3, captured.UsuarioId);
        Assert.Equal(StockMovementType.Ajuste, captured.TipoMovimiento);
        Assert.Equal(8m, captured.Cantidad);
        Assert.Equal(18m, captured.StockResultante);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveRequestAsync_RecordsExactlyOneAprobacionSolicitudMovement()
    {
        var material = new Material { Id = 1, Name = "Tela", Stock = 50m, Unit = MaterialUnit.Metros };
        var request = new MaterialRequest
        {
            Id = 10,
            MaterialId = 1,
            Material = material,
            Quantity = 12m,
            ProductionOrderId = 1,
            Status = RequestStatus.Pendiente
        };
        _requests.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        StockMovement? captured = null;
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var result = await CreateInventorySut().ApproveRequestAsync(10, actorUserId: 7);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(1, captured!.MaterialId);
        Assert.Equal(7, captured.UsuarioId);
        Assert.Equal(StockMovementType.AprobacionSolicitud, captured.TipoMovimiento);
        Assert.Equal(12m, captured.Cantidad);
        Assert.Equal(38m, captured.StockResultante);
        Assert.Equal("MaterialRequest:10", captured.Referencia);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeliverAsync_RecordsExactlyOneSalidaMovement()
    {
        var material = new Material { Id = 5, Name = "Hilo", Unit = MaterialUnit.Metros, Stock = 50m };
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-101",
            Status = OrderStatus.EnProceso,
            MaterialsStatus = OrderMaterialsStatus.PendienteRevisionBodega
        };
        var line = new ProductionOrderMaterialRequirement
        {
            Id = 9,
            ProductionOrderId = 1,
            MaterialId = 5,
            Material = material,
            QuantityRequired = 20m,
            QuantityDelivered = 0m,
            Unit = MaterialUnit.Metros
        };

        var orders = new Mock<IProductionOrderRepository>();
        var reqs = new Mock<IOrderMaterialRequirementRepository>();
        var materials = new Mock<IMaterialRepository>();
        var snapshots = new Mock<IProductionOrderBomSnapshotRepository>();
        var stockMovements = new Mock<IStockMovementRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (action, ct) => await action(ct));
        orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        reqs.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([line]);

        IReadOnlyList<StockMovement>? captured = null;
        stockMovements
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<StockMovement>, CancellationToken>((ms, _) => captured = ms.ToList())
            .Returns(Task.CompletedTask);

        var sut = new OrderMaterialService(
            orders.Object, reqs.Object, materials.Object, snapshots.Object, stockMovements.Object, uow.Object);

        var result = await sut.DeliverAsync(
            new DeliverOrderMaterialsDto(1, [new DeliverOrderMaterialItemDto(9, 20)], null),
            bodegueroId: 3);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Single(captured!);
        var mov = captured![0];
        Assert.Equal(5, mov.MaterialId);
        Assert.Equal(3, mov.UsuarioId);
        Assert.Equal(StockMovementType.Salida, mov.TipoMovimiento);
        Assert.Equal(20m, mov.Cantidad);
        Assert.Equal(30m, mov.StockResultante);
        Assert.Equal("Orden:1", mov.Referencia);
        stockMovements.Verify(
            r => r.AddRangeAsync(It.IsAny<IEnumerable<StockMovement>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveDetalleAsync_RecordsExactlyOneAprobacionSolicitudMovement()
    {
        var material = new Material { Id = 5, Name = "Tela", Stock = 100m, Unit = MaterialUnit.Metros };
        var solicitud = new SolicitudMaterial
        {
            Id = 20,
            Codigo = "SOL-0001",
            FichaId = 1,
            SolicitanteId = 2,
            Estado = SolicitudMaterialEstado.Pendiente
        };
        var detalle = new DetalleSolicitudMaterial
        {
            Id = 1,
            SolicitudMaterialId = 20,
            SolicitudMaterial = solicitud,
            MaterialId = 5,
            Material = material,
            CantidadSolicitada = 40m,
            EstadoItem = DetalleSolicitudEstado.Pendiente
        };
        solicitud.Detalles.Add(detalle);

        var solicitudRepo = new Mock<ISolicitudMaterialRepository>();
        var materialRepo = new Mock<IMaterialRepository>();
        var stockMovements = new Mock<IStockMovementRepository>();
        var codigo = new Mock<ICodigoGeneradorService>();
        var alerts = new Mock<IAlertService>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (action, ct) => await action(ct));
        solicitudRepo.Setup(r => r.GetDetalleByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(detalle);

        StockMovement? captured = null;
        stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var sut = new SolicitudMaterialApprovalService(
            solicitudRepo.Object,
            materialRepo.Object,
            stockMovements.Object,
            codigo.Object,
            alerts.Object,
            uow.Object);

        var result = await sut.ApproveDetalleAsync(1, cantidadAprobada: 40, resueltoPorId: 9);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(5, captured!.MaterialId);
        Assert.Equal(9, captured.UsuarioId);
        Assert.Equal(StockMovementType.AprobacionSolicitud, captured.TipoMovimiento);
        Assert.Equal(40m, captured.Cantidad);
        Assert.Equal(60m, captured.StockResultante);
        Assert.Equal("SolicitudMaterial:20", captured.Referencia);
        stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
