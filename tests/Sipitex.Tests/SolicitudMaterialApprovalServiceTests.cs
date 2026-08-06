using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class SolicitudMaterialApprovalServiceTests
{
    private readonly Mock<ISolicitudMaterialRepository> _solicitudRepository = new();
    private readonly Mock<IMaterialRepository> _materialRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SolicitudMaterialApprovalService CreateSut()
    {
        // La transacción mockeada solo ejecuta la acción (sin DB real)
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                async (action, ct) => await action(ct));

        return new SolicitudMaterialApprovalService(
            _solicitudRepository.Object,
            _materialRepository.Object,
            _unitOfWork.Object);
    }

    private static DetalleSolicitudMaterial CreatePendiente(
        decimal stock,
        decimal cantidadSolicitada,
        int detalleId = 1)
    {
        var material = new Material
        {
            Id = 5,
            Name = "Tela",
            Stock = stock,
            Unit = MaterialUnit.Metros
        };

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
            Id = detalleId,
            SolicitudMaterialId = solicitud.Id,
            SolicitudMaterial = solicitud,
            MaterialId = material.Id,
            Material = material,
            CantidadSolicitada = cantidadSolicitada,
            EstadoItem = DetalleSolicitudEstado.Pendiente
        };

        solicitud.Detalles.Add(detalle);
        return detalle;
    }

    [Fact]
    public async Task ApproveDetalleAsync_AprobacionTotal_DescuentaStockYActualizaEstados()
    {
        var detalle = CreatePendiente(stock: 100, cantidadSolicitada: 40);
        _solicitudRepository
            .Setup(r => r.GetDetalleByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detalle);

        var result = await CreateSut().ApproveDetalleAsync(1, cantidadAprobada: 40, resueltoPorId: 9);

        Assert.True(result.Success);
        Assert.Equal(60m, detalle.Material.Stock);
        Assert.Equal(40m, detalle.CantidadAprobada);
        Assert.Equal(DetalleSolicitudEstado.Aprobado, detalle.EstadoItem);
        Assert.Equal(SolicitudMaterialEstado.AprobadaTotal, detalle.SolicitudMaterial.Estado);
        Assert.Equal(9, detalle.SolicitudMaterial.ResueltoPorId);
        Assert.NotNull(detalle.SolicitudMaterial.FechaResolucion);
        _materialRepository.Verify(r => r.Update(detalle.Material), Times.Once);
        _solicitudRepository.Verify(r => r.Update(detalle.SolicitudMaterial), Times.Once);
        _unitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveDetalleAsync_AprobacionParcial_DescuentaSoloAprobadoYMarcaItem()
    {
        var detalle = CreatePendiente(stock: 100, cantidadSolicitada: 40);
        _solicitudRepository
            .Setup(r => r.GetDetalleByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detalle);

        var result = await CreateSut().ApproveDetalleAsync(1, cantidadAprobada: 15, resueltoPorId: 9);

        Assert.True(result.Success);
        Assert.Equal(85m, detalle.Material.Stock);
        Assert.Equal(15m, detalle.CantidadAprobada);
        Assert.Equal(DetalleSolicitudEstado.AprobadoParcial, detalle.EstadoItem);
        Assert.Equal(SolicitudMaterialEstado.AprobadaParcial, detalle.SolicitudMaterial.Estado);
        _unitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveDetalleAsync_StockInsuficiente_FallaSinModificarStockNiEstado()
    {
        var detalle = CreatePendiente(stock: 10, cantidadSolicitada: 40);
        _solicitudRepository
            .Setup(r => r.GetDetalleByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detalle);

        var result = await CreateSut().ApproveDetalleAsync(1, cantidadAprobada: 25, resueltoPorId: 9);

        Assert.False(result.Success);
        Assert.Equal(10m, detalle.Material.Stock);
        Assert.Null(detalle.CantidadAprobada);
        Assert.Equal(DetalleSolicitudEstado.Pendiente, detalle.EstadoItem);
        Assert.Equal(SolicitudMaterialEstado.Pendiente, detalle.SolicitudMaterial.Estado);
        _materialRepository.Verify(r => r.Update(It.IsAny<Material>()), Times.Never);
        _unitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
