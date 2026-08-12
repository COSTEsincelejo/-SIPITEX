using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
/// Gaps #3 y #13 (AUDITORIA_ROLES_FUNCIONES): edición completa de material (Admin)
/// y tipificación de origen en entradas de stock.
/// </summary>
public class InventoryEditAndEntryOriginTests
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
    public async Task UpdateMaterialAsync_UpdatesNameUnitAndMinStock()
    {
        var material = new Material
        {
            Id = 3,
            Name = "Tela vieja",
            Unit = MaterialUnit.Metros,
            MinStock = 10,
            Stock = 40
        };
        _materials.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().UpdateMaterialAsync(
            new UpdateMaterialDto(3, "Tela jersey premium", MaterialUnit.Kg, 5));

        Assert.True(result.Success);
        Assert.Equal("Tela jersey premium", material.Name);
        Assert.Equal(MaterialUnit.Kg, material.Unit);
        Assert.Equal(5m, material.MinStock);
        Assert.Equal(40m, material.Stock); // stock no cambia
        _materials.Verify(r => r.Update(material), Times.Once);
        _stockMovements.Verify(
            r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateMaterialAsync_WhenNotFound_Fails()
    {
        _materials.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Material?)null);

        var result = await CreateSut().UpdateMaterialAsync(
            new UpdateMaterialDto(99, "X", MaterialUnit.Metros, 1));

        Assert.False(result.Success);
        Assert.Contains("no encontrado", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateMaterialAsync_WhenNameBlank_Fails()
    {
        var result = await CreateSut().UpdateMaterialAsync(
            new UpdateMaterialDto(1, "   ", MaterialUnit.Metros, 1));

        Assert.False(result.Success);
        _materials.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void InventarioController_EditMaterial_IsAdministradorOnly()
    {
        var method = typeof(InventarioController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nameof(InventarioController.EditMaterial));

        var attr = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(UserRoles.Administrador, attr!.Roles);
        Assert.DoesNotContain(UserRoles.Instructor, attr.Roles!, StringComparison.Ordinal);
        Assert.DoesNotContain(UserRoles.Bodeguero, attr.Roles!, StringComparison.Ordinal);
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
    }

    [Theory]
    [InlineData(StockEntryOrigin.Compra)]
    [InlineData(StockEntryOrigin.Devolucion)]
    [InlineData(StockEntryOrigin.OtraFuenteAutorizada)]
    public async Task AddMaterialAsync_RecordsEntradaWithCorrectOrigen(StockEntryOrigin origen)
    {
        StockMovement? captured = null;
        _materials
            .Setup(r => r.AddAsync(It.IsAny<Material>(), It.IsAny<CancellationToken>()))
            .Callback<Material, CancellationToken>((m, _) => m.Id = 21)
            .Returns(Task.CompletedTask);
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateSut().AddMaterialAsync(
            new CreateMaterialDto("Hilo", 12m, MaterialUnit.Metros, origen),
            actorUserId: 5);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(StockMovementType.Entrada, captured!.TipoMovimiento);
        Assert.Equal(origen, captured.Origen);
        Assert.Equal(12m, captured.Cantidad);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenIncrease_RequiresAndRecordsOrigen()
    {
        var material = new Material { Id = 4, Name = "Hilo", Stock = 10m, Unit = MaterialUnit.Metros };
        _materials.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        StockMovement? captured = null;
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var missing = await CreateSut().AdjustStockAsync(new AdjustStockDto(4, 18m), actorUserId: 3);
        Assert.False(missing.Success);
        Assert.Contains("origen", missing.Message, StringComparison.OrdinalIgnoreCase);

        var ok = await CreateSut().AdjustStockAsync(
            new AdjustStockDto(4, 18m, StockEntryOrigin.Devolucion),
            actorUserId: 3);

        Assert.True(ok.Success);
        Assert.NotNull(captured);
        Assert.Equal(StockMovementType.Ajuste, captured!.TipoMovimiento);
        Assert.Equal(StockEntryOrigin.Devolucion, captured.Origen);
        Assert.Equal(8m, captured.Cantidad);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenDecrease_DoesNotRequireOrigen()
    {
        var material = new Material { Id = 4, Name = "Hilo", Stock = 10m, Unit = MaterialUnit.Metros };
        _materials.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(material);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        StockMovement? captured = null;
        _stockMovements
            .Setup(r => r.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()))
            .Callback<StockMovement, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().AdjustStockAsync(new AdjustStockDto(4, 4m), actorUserId: 3);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Null(captured!.Origen);
        Assert.Equal(6m, captured.Cantidad);
    }
}
