using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

/// <summary>
/// Gap #14 (AUDITORIA_ROLES_FUNCIONES): reingreso EncargadoDeBodega desde etapas MES.
/// </summary>
public class StageReentryTests
{
    private readonly Mock<IProductionOrderRepository> _orders = new();
    private readonly Mock<IProductionFlowRepository> _flow = new();
    private readonly Mock<IOrderMaterialRequirementRepository> _reqMaterials = new();
    private readonly Mock<IProductionOrderBomSnapshotRepository> _snapshots = new();
    private readonly Mock<IBomRepository> _boms = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMaterialRepository> _materials = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<IConsumoMaterialRepository> _consumos = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ProductionFlowService CreateSut() => new(
        _orders.Object,
        _flow.Object,
        _reqMaterials.Object,
        _snapshots.Object,
        _boms.Object,
        _users.Object,
        _materials.Object,
        _stockMovements.Object,
        _uow.Object,
        new Mock<IProductLifecycleService>().Object,
        NullLogger<ProductionFlowService>.Instance,
        _consumos.Object);

    private static ProductionOrderStage CreateStage(int id, string name, int orderId, int available = 40) =>
        new()
        {
            Id = id,
            ProductionOrderId = orderId,
            Name = name,
            SortOrder = Array.IndexOf(ProductionFlowService.DefaultStageNames, name) + 1,
            QuantityReceived = available,
            QuantitySent = 0,
            QuantityWithdrawn = 0,
            QuantityProcessed = 0
        };

    public static IEnumerable<object[]> DefaultStages() =>
        ProductionFlowService.DefaultStageNames.Select((n, i) => new object[] { n, i + 1 });

    [Theory]
    [MemberData(nameof(DefaultStages))]
    public async Task EncargadoDeBodega_ReingresoMaterial_FromEachDefaultStage_UpdatesStockAndLedger(
        string stageName, int stageId)
    {
        var order = new ProductionOrder
        {
            Id = 10,
            OrderNumber = "OP-210",
            ProductName = "Camisa",
            TotalQuantity = 100,
            ProducedQuantity = 0,
            Status = OrderStatus.EnProceso
        };
        var stage = CreateStage(stageId, stageName, order.Id, available: 25);
        var material = new Material
        {
            Id = 4,
            Name = "Tela Jersey",
            Stock = 100m,
            Unit = MaterialUnit.Metros,
            MinStock = 10,
            Status = MaterialStatus.Bueno,
            CostoPromedioPonderado = 4m
        };
        var linea = new ProductionOrderMaterialRequirement
        {
            Id = 40,
            ProductionOrderId = 10,
            MaterialId = 4,
            QuantityDelivered = 25m,
            Material = material
        };

        _flow.Setup(f => f.GetStageByIdAsync(stageId, It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        _orders.Setup(o => o.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _materials.Setup(m => m.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _reqMaterials.Setup(r => r.GetByOrderIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([linea]);
        _consumos.Setup(c => c.GetByOrderIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (action, ct) => await action(ct));

        StockMovement? captured = null;
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(10, stageId, Quantity: 5, MaterialId: 4, Observations: "Retorno"),
            actorUserId: 8,
            actorName: "PlantaInventario",
            actorRole: UserRoles.EncargadoDeBodega);

        Assert.True(result.Success, result.Message);
        Assert.Equal(105m, material.Stock);
        Assert.Equal(0, stage.QuantityWithdrawn);
        Assert.Equal(5m, linea.QuantityReentered);
        Assert.Equal(4m, material.CostoPromedioPonderado);
        Assert.NotNull(captured);
        Assert.Equal(4m, captured!.CostoUnitario);
        Assert.Equal(StockMovementType.Entrada, captured!.TipoMovimiento);
        Assert.Equal(4, captured.MaterialId);
        Assert.Equal(8, captured.UsuarioId);
        Assert.Equal(5m, captured.Cantidad);
        Assert.Equal(105m, captured.StockResultante);
        Assert.Equal($"Orden:10/Etapa:{stageName}", captured.Referencia);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterStageReentry_ProductoTerminado_UsesFinishedGoodPathWithoutStockMovement()
    {
        var stage = CreateStage(3, "Confección", 2, available: 40);
        var order = new ProductionOrder
        {
            Id = 2,
            ProductName = "Camisa",
            TotalQuantity = 100,
            ProducedQuantity = 10,
            Status = OrderStatus.EnProceso
        };

        _flow.Setup(f => f.GetStageByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        _orders.Setup(o => o.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _flow.Setup(f => f.GetFinishedGoodAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinishedGoodStock?)null);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (action, ct) => await action(ct));

        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(2, 3, 15, MaterialId: null, Observations: "Lote"),
            8, "PlantaInventario", UserRoles.EncargadoDeBodega);

        Assert.True(result.Success);
        Assert.Equal(15, stage.QuantityWithdrawn);
        Assert.Equal(25, order.ProducedQuantity);
        _flow.Verify(f => f.AddFinishedGoodAsync(It.Is<FinishedGoodStock>(s => s.Stock == 15), It.IsAny<CancellationToken>()), Times.Once);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterStageReentry_Instructor_IsRejected()
    {
        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(10, 1, 2, MaterialId: 4, null),
            3, "Inst", UserRoles.Instructor);

        Assert.False(result.Success);
        Assert.Contains("Encargado de bodega", result.Message);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _materials.Verify(m => m.Update(It.IsAny<Material>()), Times.Never);
    }

    [Fact]
    public void PlantasInventarioOrdenesController_ReingresoActions_AreEncargadoDeBodegaOrAdmin_NotInstructor()
    {
        var classAttr = typeof(PlantasInventarioOrdenesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAttr);
        Assert.Contains(UserRoles.EncargadoDeBodega, classAttr!.Roles!, StringComparison.Ordinal);
        Assert.Contains(UserRoles.Administrador, classAttr.Roles!, StringComparison.Ordinal);
        Assert.Contains(UserRoles.Instructor, classAttr.Roles!, StringComparison.Ordinal);

        var reingresoMethods = typeof(PlantasInventarioOrdenesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == nameof(PlantasInventarioOrdenesController.Reingreso))
            .ToList();
        Assert.Equal(2, reingresoMethods.Count);

        foreach (var method in reingresoMethods)
        {
            var methodAttr = method.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(methodAttr);
            Assert.Contains(UserRoles.EncargadoDeBodega, methodAttr!.Roles!, StringComparison.Ordinal);
            Assert.Contains(UserRoles.Administrador, methodAttr.Roles!, StringComparison.Ordinal);
            Assert.DoesNotContain(UserRoles.Instructor, methodAttr.Roles!, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task PartialInventoryIn_Admin_StillWorksUnchanged()
    {
        var stage = CreateStage(5, "Confección", 2, available: 40);
        var order = new ProductionOrder
        {
            Id = 2, ProductName = "Camisa", TotalQuantity = 100, ProducedQuantity = 10, Status = OrderStatus.EnProceso
        };
        _flow.Setup(f => f.GetStageByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        _orders.Setup(o => o.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _flow.Setup(f => f.GetFinishedGoodAsync("Camisa", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinishedGoodStock?)null);

        var result = await CreateSut().PartialInventoryInAsync(
            new PartialInventoryInDto(2, 5, 15, "Lote A"),
            1, "Admin", UserRoles.Administrador);

        Assert.True(result.Success);
        Assert.Equal(15, stage.QuantityWithdrawn);
        Assert.Equal(25, order.ProducedQuantity);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReingresarMasDeLoDisponible_FallaConElDetalle()
    {
        var ctx = PrepararMaterial(entregado: 10, consumido: 3, stock: 7, promedio: 5);
        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(1, 3, 8, 5, null), 8, "Bodega", UserRoles.Administrador);

        Assert.False(result.Success);
        Assert.Contains("Solo se pueden reingresar 7", result.Message, StringComparison.Ordinal);
        Assert.Contains("se entregaron 10", result.Message, StringComparison.Ordinal);
        Assert.Contains("se consumieron 3", result.Message, StringComparison.Ordinal);
        Assert.Contains("ya se reingresaron 0", result.Message, StringComparison.Ordinal);
        Assert.Equal(7m, ctx.Material.Stock);
        Assert.Equal(0m, ctx.Linea.QuantityReentered);
        Assert.Equal(0, ctx.Stage.QuantityWithdrawn);
    }

    [Fact]
    public async Task ReingresarMaterialAjeno_Falla()
    {
        PrepararMaterial(entregado: 10, consumido: 0, stock: 10, promedio: 5);
        _materials.Setup(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Material { Id = 99, Name = "Botón", Stock = 4 });

        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(1, 3, 1, 99, null), 8, "Bodega", UserRoles.Administrador);

        Assert.False(result.Success);
        Assert.Contains("no está en la orden", result.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OrderStatus.Finalizada)]
    [InlineData(OrderStatus.Cancelada)]
    public async Task ReingresarEnOrdenCerrada_Funciona(OrderStatus estado)
    {
        var ctx = PrepararMaterial(entregado: 4, consumido: 0, stock: 6, promedio: 2, estado: estado);
        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(1, 3, 4, 5, null), 8, "Bodega", UserRoles.EncargadoDeBodega);

        Assert.True(result.Success, result.Message);
        Assert.Equal(10m, ctx.Material.Stock);
        Assert.Equal(4m, ctx.Linea.QuantityReentered);
        Assert.Equal(estado, ctx.Order.Status);
    }

    [Fact]
    public async Task ReingresarCantidadDecimal_Funciona()
    {
        var ctx = PrepararMaterial(entregado: 10, consumido: 0, stock: 20, promedio: 8);
        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(1, 3, 3.20m, 5, null), 8, "Bodega", UserRoles.Administrador);

        Assert.True(result.Success, result.Message);
        Assert.Equal(23.20m, ctx.Material.Stock);
        Assert.Equal(3.20m, ctx.Linea.QuantityReentered);
        Assert.Equal(8m, ctx.Material.CostoPromedioPonderado);
        Assert.Equal(0, ctx.Stage.QuantityWithdrawn);
    }

    [Fact]
    public async Task ReingresarConPromedioCero_VuelveAStockSinCosto()
    {
        var ctx = PrepararMaterial(entregado: 5, consumido: 0, stock: 5, promedio: 0, stockSinCosto: 5);
        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(1, 3, 2, 5, null), 8, "Bodega", UserRoles.Administrador);

        Assert.True(result.Success, result.Message);
        Assert.Equal(7m, ctx.Material.Stock);
        Assert.Equal(7m, ctx.Material.StockSinCosto);
        Assert.Equal(0m, ctx.Material.CostoPromedioPonderado);
    }

    [Fact]
    public async Task ReingresarDespuesDeConsumir_NoPasaDelDisponible()
    {
        var ctx = PrepararMaterial(entregado: 10, consumido: 3, stock: 10, promedio: 5);
        var deMas = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(1, 3, 8, 5, null), 8, "Bodega", UserRoles.Administrador);
        Assert.False(deMas.Success);

        var justo = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(1, 3, 7, 5, null), 8, "Bodega", UserRoles.Administrador);
        Assert.True(justo.Success, justo.Message);
        Assert.Equal(17m, ctx.Material.Stock);
        Assert.Equal(0, ctx.Stage.QuantityWithdrawn);
    }

    private (ProductionOrder Order, ProductionOrderStage Stage, Material Material, ProductionOrderMaterialRequirement Linea) PrepararMaterial(
        decimal entregado,
        decimal consumido,
        decimal stock,
        decimal promedio,
        OrderStatus estado = OrderStatus.EnProceso,
        decimal stockSinCosto = 0)
    {
        var order = new ProductionOrder { Id = 1, OrderNumber = "OP-1", ProductName = "Camisa", Status = estado };
        var stage = CreateStage(3, "Confección", 1, available: 40);
        var material = new Material
        {
            Id = 5,
            Name = "Tela",
            Stock = stock,
            StockSinCosto = stockSinCosto,
            CostoPromedioPonderado = promedio,
            Unit = MaterialUnit.Metros
        };
        var linea = new ProductionOrderMaterialRequirement
        {
            Id = 9,
            ProductionOrderId = 1,
            MaterialId = 5,
            Material = material,
            QuantityRequired = entregado,
            QuantityDelivered = entregado
        };
        var previos = consumido <= 0
            ? new List<ConsumoMaterial>()
            : new List<ConsumoMaterial>
            {
                new()
                {
                    ProductionOrderId = 1,
                    MaterialId = 5,
                    OrderMaterialRequirementId = 9,
                    Cantidad = consumido
                }
            };

        _orders.Setup(o => o.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _flow.Setup(f => f.GetStageByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        _materials.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _reqMaterials.Setup(r => r.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([linea]);
        _consumos.Setup(c => c.GetByOrderIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(previos);
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (action, ct) => await action(ct));
        _stockMovements.Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (order, stage, material, linea);
    }
}
