using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class EstadoProductoCicloTests
{
    [Fact]
    public async Task CompletarTrazo_PoneCorteEnLaOrdenEnLasPrendasYEnTrazabilidad()
    {
        var trazo = new ProductionOrderStage
        {
            Id = 10,
            ProductionOrderId = 1,
            Name = "Trazo",
            SortOrder = 1,
            Status = ProductionStageStatus.EnProceso,
            QuantityReceived = 4
        };
        var corte = new ProductionOrderStage
        {
            Id = 11,
            ProductionOrderId = 1,
            Name = "Corte",
            SortOrder = 2,
            Status = ProductionStageStatus.Pendiente
        };
        var order = new ProductionOrder
        {
            Id = 1,
            OrderNumber = "OP-101",
            ProductName = "Camisa",
            Status = OrderStatus.EnProceso,
            EstadoProducto = EstadoProducto.MateriaPrima,
            CurrentStageId = trazo.Id,
            TotalQuantity = 4
        };
        var prendas = new List<PrendaTrazable>
        {
            new()
            {
                Id = 3,
                Codigo = "SIP-OP-101-0001",
                ProductionOrderId = 1,
                ProductName = "Camisa",
                Estado = EstadoProducto.MateriaPrima,
                ProductionOrder = order,
                CreadoPor = new User { Id = 1, Nombre = "Ana" }
            },
            new()
            {
                Id = 4,
                Codigo = "SIP-OP-101-0002",
                ProductionOrderId = 1,
                ProductName = "Camisa",
                Estado = EstadoProducto.MateriaPrima,
                ProductionOrder = order,
                CreadoPor = new User { Id = 1, Nombre = "Ana" }
            }
        };

        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var flow = new Mock<IProductionFlowRepository>();
        flow.Setup(f => f.GetStageByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(trazo);
        flow.Setup(f => f.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([trazo, corte]);
        flow.Setup(f => f.AddHistoryAsync(It.IsAny<ProductionOrderHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        flow.Setup(f => f.GetHistoryByOrderAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var prendasRepo = new Mock<IPrendaTrazableRepository>();
        prendasRepo.Setup(r => r.GetByOrderForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(prendas);
        prendasRepo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(prendas[0]);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var lifecycle = new ProductLifecycleService(
            new ProductStateMachine(),
            orders.Object,
            prendasRepo.Object,
            flow.Object,
            new Mock<IActivityLogService>().Object,
            uow.Object);
        var sut = new ProductionFlowService(
            orders.Object,
            flow.Object,
            new Mock<IOrderMaterialRequirementRepository>().Object,
            new Mock<IProductionOrderBomSnapshotRepository>().Object,
            new Mock<IBomRepository>().Object,
            new Mock<IUserRepository>().Object,
            new Mock<IMaterialRepository>().Object,
            new Mock<IStockMovementRepository>().Object,
            uow.Object,
            lifecycle,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProductionFlowService>.Instance);

        var result = await sut.CompleteStageAsync(10, 1, "Admin", UserRoles.Administrador);

        Assert.True(result.Success, result.Message);
        Assert.Equal(corte.Id, order.CurrentStageId);
        Assert.Equal(EstadoProducto.Corte, order.EstadoProducto);
        Assert.All(prendas, p => Assert.Equal(EstadoProducto.Corte, p.Estado));

        var snaps = new Mock<IProductionOrderBomSnapshotRepository>();
        snaps.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var boms = new Mock<IBomRepository>();
        boms.Setup(r => r.GetByProductAsync("Camisa", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var consumos = new Mock<IConsumoMaterialRepository>();
        consumos.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var quality = new Mock<IQualityRepository>();
        quality.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var trazabilidad = new TrazabilidadService(
            prendasRepo.Object, orders.Object, snaps.Object, boms.Object, consumos.Object,
            quality.Object, flow.Object, new Mock<IUserRepository>().Object,
            new Mock<IActivityLogService>().Object, uow.Object);

        var detail = await trazabilidad.GetByIdAsync(new TrazabilidadViewerFilter(UserRoles.Administrador, 1, []), 3);

        Assert.NotNull(detail);
        Assert.Equal(EstadoProducto.Corte, detail!.Estado);
        Assert.Equal("Corte", detail.EtapaActual);
        Assert.NotEqual(EstadoProducto.MateriaPrima, detail.Estado);
    }

    [Fact]
    public async Task EnviarACorte_AlineaElCicloConLaEtapaDestino()
    {
        var trazo = new ProductionOrderStage
        {
            Id = 10,
            ProductionOrderId = 1,
            Name = "Trazo",
            SortOrder = 1,
            Status = ProductionStageStatus.EnProceso,
            QuantityReceived = 4
        };
        var corte = new ProductionOrderStage
        {
            Id = 11,
            ProductionOrderId = 1,
            Name = "Corte",
            SortOrder = 2
        };
        var order = new ProductionOrder
        {
            Id = 1,
            Status = OrderStatus.EnProceso,
            EstadoProducto = EstadoProducto.MateriaPrima,
            CurrentStageId = trazo.Id
        };
        var prenda = new PrendaTrazable { Id = 3, ProductionOrderId = 1, Estado = EstadoProducto.MateriaPrima };
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var flow = new Mock<IProductionFlowRepository>();
        flow.Setup(f => f.GetStageByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(trazo);
        flow.Setup(f => f.GetStagesByOrderAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([trazo, corte]);
        flow.Setup(f => f.AddMovementAsync(It.IsAny<ProductionOrderStageMovement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        flow.Setup(f => f.AddHistoryAsync(It.IsAny<ProductionOrderHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var prendas = new Mock<IPrendaTrazableRepository>();
        prendas.Setup(r => r.GetByOrderForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([prenda]);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new ProductionFlowService(
            orders.Object, flow.Object,
            new Mock<IOrderMaterialRequirementRepository>().Object,
            new Mock<IProductionOrderBomSnapshotRepository>().Object,
            new Mock<IBomRepository>().Object,
            new Mock<IUserRepository>().Object,
            new Mock<IMaterialRepository>().Object,
            new Mock<IStockMovementRepository>().Object,
            uow.Object,
            new ProductLifecycleService(
                new ProductStateMachine(), orders.Object, prendas.Object, flow.Object,
                new Mock<IActivityLogService>().Object, uow.Object),
            NullLogger<ProductionFlowService>.Instance);

        var result = await sut.SendToNextAsync(
            new SendToNextStageDto(10, 2, null), 1, "Admin", UserRoles.Administrador);

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, trazo.QuantitySent);
        Assert.Equal(2, corte.QuantityReceived);
        Assert.Equal(EstadoProducto.Corte, order.EstadoProducto);
        Assert.Equal(EstadoProducto.Corte, prenda.Estado);
        flow.Verify(f => f.AddMovementAsync(
            It.Is<ProductionOrderStageMovement>(m => m.MovementType == "Send" && m.Quantity == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalidadMalo_VuelveAConfeccionYReabreEsaEtapa()
    {
        var confeccion = new ProductionOrderStage
        {
            Id = 20,
            ProductionOrderId = 2,
            Name = "Confección",
            SortOrder = 3,
            Status = ProductionStageStatus.Finalizado,
            CompletedAtUtc = DateTime.UtcNow
        };
        var calidad = new ProductionOrderStage
        {
            Id = 21,
            ProductionOrderId = 2,
            Name = "Control de Calidad",
            SortOrder = 4,
            Status = ProductionStageStatus.EnProceso
        };
        var order = new ProductionOrder
        {
            Id = 2,
            OrderNumber = "OP-202",
            Status = OrderStatus.EnProceso,
            EstadoProducto = EstadoProducto.Calidad,
            CurrentStageId = calidad.Id
        };
        var prenda = new PrendaTrazable
        {
            Id = 8,
            ProductionOrderId = 2,
            Estado = EstadoProducto.Calidad
        };
        var orders = new Mock<IProductionOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var flow = new Mock<IProductionFlowRepository>();
        flow.Setup(f => f.GetStagesByOrderAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([confeccion, calidad]);
        var prendas = new Mock<IPrendaTrazableRepository>();
        prendas.Setup(r => r.GetByOrderForUpdateAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync([prenda]);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var log = new Mock<IActivityLogService>();
        log.Setup(l => l.LogAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var lifecycle = new ProductLifecycleService(
            new ProductStateMachine(), orders.Object, prendas.Object, flow.Object, log.Object, uow.Object);

        var result = await lifecycle.TransitionAsync(2, EstadoProducto.Confeccion, 7, "Clasificación Malo");

        Assert.True(result.Success, result.Message);
        Assert.Equal(EstadoProducto.Confeccion, order.EstadoProducto);
        Assert.Equal(EstadoProducto.Confeccion, prenda.Estado);
        Assert.Equal(confeccion.Id, order.CurrentStageId);
        Assert.Equal(ProductionStageStatus.EnProceso, confeccion.Status);
        Assert.Null(confeccion.CompletedAtUtc);
    }

    [Fact]
    public async Task OrdenCancelada_NoCambiaElCiclo()
    {
        var order = new ProductionOrder
        {
            Id = 3,
            Status = OrderStatus.Cancelada,
            EstadoProducto = EstadoProducto.Corte
        };
        var prenda = new PrendaTrazable { ProductionOrderId = 3, Estado = EstadoProducto.Corte };
        var prendas = new Mock<IPrendaTrazableRepository>();
        prendas.Setup(r => r.GetByOrderForUpdateAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync([prenda]);
        var lifecycle = new ProductLifecycleService(
            new ProductStateMachine(),
            new Mock<IProductionOrderRepository>().Object,
            prendas.Object,
            new Mock<IProductionFlowRepository>().Object,
            new Mock<IActivityLogService>().Object,
            new Mock<IUnitOfWork>().Object);

        await lifecycle.AlignWithStageAsync(order, "Terminado");

        Assert.Equal(EstadoProducto.Corte, order.EstadoProducto);
        Assert.Equal(EstadoProducto.Corte, prenda.Estado);
    }

    [Theory]
    [InlineData("Trazo", EstadoProducto.MateriaPrima)]
    [InlineData("Corte", EstadoProducto.Corte)]
    [InlineData("Confección", EstadoProducto.Confeccion)]
    [InlineData("Control de Calidad", EstadoProducto.Calidad)]
    [InlineData("Terminado", EstadoProducto.ProductoTerminado)]
    public void MapeoDeEtapasConocidas(string etapa, EstadoProducto esperado)
    {
        Assert.True(EstadoProductoEtapa.TryForStage(etapa, out var estado));
        Assert.Equal(esperado, estado);
    }

    [Fact]
    public void EtapaDesconocida_NoInventaEstado()
    {
        Assert.False(EstadoProductoEtapa.TryForStage("Bordado", out _));
    }
}
