using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Web.Controllers;

namespace Sipitex.Tests;

/// <summary>
/// Gap #14 (AUDITORIA_ROLES_FUNCIONES): reingreso Bodeguero desde etapas MES.
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
        _uow.Object);

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
    public async Task Bodeguero_ReingresoMaterial_FromEachDefaultStage_UpdatesStockAndLedger(
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
            Status = MaterialStatus.Bueno
        };

        _flow.Setup(f => f.GetStageByIdAsync(stageId, It.IsAny<CancellationToken>())).ReturnsAsync(stage);
        _orders.Setup(o => o.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _materials.Setup(m => m.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        StockMovement? captured = null;
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(10, stageId, Quantity: 5, MaterialId: 4, Observations: "Retorno"),
            actorUserId: 8,
            actorName: "Bodega",
            actorRole: UserRoles.Bodeguero);

        Assert.True(result.Success);
        Assert.Equal(105m, material.Stock);
        Assert.Equal(5, stage.QuantityWithdrawn);
        Assert.NotNull(captured);
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

        var result = await CreateSut().RegisterStageReentryAsync(
            new StageReentryDto(2, 3, 15, MaterialId: null, Observations: "Lote"),
            8, "Bodega", UserRoles.Bodeguero);

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
        Assert.Contains("Bodeguero", result.Message);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _materials.Verify(m => m.Update(It.IsAny<Material>()), Times.Never);
    }

    [Fact]
    public void BodegaOrdenesController_ReingresoActions_AreBodegueroOnly_NotInstructor()
    {
        var classAttr = typeof(BodegaOrdenesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(classAttr);
        Assert.Equal(UserRoles.Bodeguero, classAttr!.Roles);
        Assert.DoesNotContain(UserRoles.Instructor, classAttr.Roles!, StringComparison.Ordinal);

        var reingresoMethods = typeof(BodegaOrdenesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == nameof(BodegaOrdenesController.Reingreso))
            .ToList();
        Assert.Equal(2, reingresoMethods.Count);

        // Ningún override abre la acción a Instructor
        foreach (var method in reingresoMethods)
        {
            var methodAttr = method.GetCustomAttribute<AuthorizeAttribute>();
            if (methodAttr?.Roles is string roles)
                Assert.DoesNotContain(UserRoles.Instructor, roles, StringComparison.Ordinal);
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
}
