using Moq;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class SolicitudMaterialApprovalServiceTests
{
    private readonly Mock<ISolicitudMaterialRepository> _solicitudRepository = new();
    private readonly Mock<IMaterialRepository> _materialRepository = new();
    private readonly Mock<IStockMovementRepository> _stockMovements = new();
    private readonly Mock<ICodigoGeneradorService> _codigoGenerador = new();
    private readonly Mock<IAlertService> _alertService = new();
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

        _codigoGenerador
            .Setup(c => c.GenerarCodigoEntregaMaterialAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("ENT-0001");

        _alertService
            .Setup(a => a.NotifyUsersAsync(
                It.IsAny<AlertType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<int>?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new SolicitudMaterialApprovalService(
            _solicitudRepository.Object,
            _materialRepository.Object,
            _stockMovements.Object,
            _codigoGenerador.Object,
            _alertService.Object,
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

    private static SolicitudMaterial CreateSolicitudConDetalles(
        params (int detalleId, decimal stock, decimal solicitada)[] lineas)
    {
        var solicitud = new SolicitudMaterial
        {
            Id = 20,
            Codigo = "SOL-0010",
            Tipo = SolicitudMaterialTipo.PorFicha,
            FichaId = 1,
            SolicitanteId = 10,
            Estado = SolicitudMaterialEstado.Pendiente
        };

        foreach (var (detalleId, stock, solicitada) in lineas)
        {
            var material = new Material
            {
                Id = 100 + detalleId,
                Name = $"Mat-{detalleId}",
                Stock = stock,
                Unit = MaterialUnit.Unidades
            };
            var detalle = new DetalleSolicitudMaterial
            {
                Id = detalleId,
                SolicitudMaterialId = solicitud.Id,
                SolicitudMaterial = solicitud,
                MaterialId = material.Id,
                Material = material,
                CantidadSolicitada = solicitada,
                EstadoItem = DetalleSolicitudEstado.Pendiente
            };
            solicitud.Detalles.Add(detalle);
        }

        return solicitud;
    }

    [Fact]
    public async Task ResolveSolicitudAsync_AprobacionTotal_GeneraEntregaYDescuentaStock()
    {
        var solicitud = CreateSolicitudConDetalles((1, 100, 40), (2, 50, 10));
        _solicitudRepository
            .Setup(r => r.GetByIdWithDetallesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);

        EntregaMaterial? entrega = null;
        _solicitudRepository
            .Setup(r => r.AddEntregaAsync(It.IsAny<EntregaMaterial>(), It.IsAny<CancellationToken>()))
            .Callback<EntregaMaterial, CancellationToken>((e, _) => entrega = e)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().ResolveSolicitudAsync(
            20,
            [new ResolveDetalleDto(1, 40), new ResolveDetalleDto(2, 10)],
            bodegueroId: 9);

        Assert.True(result.Success);
        Assert.Equal(SolicitudMaterialEstado.AprobadaTotal, solicitud.Estado);
        Assert.Equal(60m, solicitud.Detalles.First(d => d.Id == 1).Material.Stock);
        Assert.Equal(40m, solicitud.Detalles.First(d => d.Id == 2).Material.Stock);
        Assert.NotNull(entrega);
        Assert.Equal("ENT-0001", entrega!.Codigo);
        Assert.Equal(9, entrega.BodegueroId);
        _alertService.Verify(a => a.NotifyUsersAsync(
            AlertType.SolicitudMaterialResuelta,
            It.IsAny<string>(),
            It.Is<string>(b => b.Contains("ENT-0001")),
            It.Is<IReadOnlyList<int>>(ids => ids.Contains(10)),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveSolicitudAsync_AprobacionParcial_DescuentaSoloAprobado()
    {
        var solicitud = CreateSolicitudConDetalles((1, 100, 40), (2, 50, 10));
        _solicitudRepository
            .Setup(r => r.GetByIdWithDetallesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);
        _solicitudRepository
            .Setup(r => r.AddEntregaAsync(It.IsAny<EntregaMaterial>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().ResolveSolicitudAsync(
            20,
            [new ResolveDetalleDto(1, 15), new ResolveDetalleDto(2, 0)],
            bodegueroId: 9);

        Assert.True(result.Success);
        Assert.Equal(SolicitudMaterialEstado.AprobadaParcial, solicitud.Estado);
        Assert.Equal(85m, solicitud.Detalles.First(d => d.Id == 1).Material.Stock);
        Assert.Equal(50m, solicitud.Detalles.First(d => d.Id == 2).Material.Stock);
        Assert.Equal(DetalleSolicitudEstado.AprobadoParcial, solicitud.Detalles.First(d => d.Id == 1).EstadoItem);
        Assert.Equal(DetalleSolicitudEstado.Rechazado, solicitud.Detalles.First(d => d.Id == 2).EstadoItem);
        _solicitudRepository.Verify(
            r => r.AddEntregaAsync(It.IsAny<EntregaMaterial>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveSolicitudAsync_RechazoTotal_SinEntregaNiDescuento()
    {
        var solicitud = CreateSolicitudConDetalles((1, 100, 40), (2, 50, 10));
        _solicitudRepository
            .Setup(r => r.GetByIdWithDetallesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);

        var result = await CreateSut().ResolveSolicitudAsync(
            20,
            [new ResolveDetalleDto(1, 0), new ResolveDetalleDto(2, 0)],
            bodegueroId: 9);

        Assert.True(result.Success);
        Assert.Equal(SolicitudMaterialEstado.Rechazada, solicitud.Estado);
        Assert.Equal(100m, solicitud.Detalles.First(d => d.Id == 1).Material.Stock);
        Assert.Equal(50m, solicitud.Detalles.First(d => d.Id == 2).Material.Stock);
        _solicitudRepository.Verify(
            r => r.AddEntregaAsync(It.IsAny<EntregaMaterial>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _materialRepository.Verify(r => r.Update(It.IsAny<Material>()), Times.Never);
        _alertService.Verify(a => a.NotifyUsersAsync(
            AlertType.SolicitudMaterialResuelta,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<IReadOnlyList<int>>(ids => ids.Contains(10)),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveSolicitudAsync_CantidadMayorQueStock_FallaSinCambios()
    {
        var solicitud = CreateSolicitudConDetalles((1, 10, 40));
        _solicitudRepository
            .Setup(r => r.GetByIdWithDetallesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);

        var result = await CreateSut().ResolveSolicitudAsync(
            20,
            [new ResolveDetalleDto(1, 25)],
            bodegueroId: 9);

        Assert.False(result.Success);
        Assert.Equal(SolicitudMaterialEstado.Pendiente, solicitud.Estado);
        Assert.Equal(10m, solicitud.Detalles.First().Material.Stock);
        Assert.Equal(DetalleSolicitudEstado.Pendiente, solicitud.Detalles.First().EstadoItem);
        _unitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _alertService.Verify(
            a => a.NotifyUsersAsync(
                It.IsAny<AlertType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<int>?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveSolicitudAsync_YaResuelta_Falla()
    {
        var solicitud = CreateSolicitudConDetalles((1, 100, 40));
        solicitud.Estado = SolicitudMaterialEstado.AprobadaTotal;
        _solicitudRepository
            .Setup(r => r.GetByIdWithDetallesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);

        var result = await CreateSut().ResolveSolicitudAsync(
            20,
            [new ResolveDetalleDto(1, 40)],
            bodegueroId: 9);

        Assert.False(result.Success);
        _unitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveSolicitudAsync_InsumosLibres_SinMapeo_NoDescuentaStock()
    {
        var material = new Material { Id = 5, Name = "Tela", Stock = 100, Unit = MaterialUnit.Metros };
        var solicitud = new SolicitudMaterial
        {
            Id = 20,
            Codigo = "SOL-LIB",
            Tipo = SolicitudMaterialTipo.InsumosLibres,
            SolicitanteId = 10,
            Estado = SolicitudMaterialEstado.Pendiente,
            Detalles =
            [
                new DetalleSolicitudMaterial
                {
                    Id = 1,
                    SolicitudMaterialId = 20,
                    MaterialId = null,
                    Material = null,
                    DescripcionItem = "Tela orión",
                    CantidadSolicitada = 2,
                    EstadoItem = DetalleSolicitudEstado.Pendiente
                }
            ]
        };
        solicitud.Detalles.First().SolicitudMaterial = solicitud;

        _solicitudRepository
            .Setup(r => r.GetByIdWithDetallesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);

        var result = await CreateSut().ResolveSolicitudAsync(
            20,
            [new ResolveDetalleDto(1, 2)],
            bodegueroId: 9);

        Assert.False(result.Success);
        Assert.Contains("mapear", result.Message!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(100m, material.Stock);
        Assert.Equal(SolicitudMaterialEstado.Pendiente, solicitud.Estado);
        _unitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveSolicitudAsync_InsumosLibres_ConMapeo_DescuentaStock()
    {
        var material = new Material { Id = 5, Name = "Tela", Stock = 100, Unit = MaterialUnit.Metros };
        var solicitud = new SolicitudMaterial
        {
            Id = 20,
            Codigo = "SOL-LIB",
            Tipo = SolicitudMaterialTipo.InsumosLibres,
            SolicitanteId = 10,
            Estado = SolicitudMaterialEstado.Pendiente,
            Detalles =
            [
                new DetalleSolicitudMaterial
                {
                    Id = 1,
                    SolicitudMaterialId = 20,
                    MaterialId = null,
                    Material = null,
                    DescripcionItem = "Tela orión",
                    CantidadSolicitada = 2,
                    EstadoItem = DetalleSolicitudEstado.Pendiente
                }
            ]
        };
        solicitud.Detalles.First().SolicitudMaterial = solicitud;

        _solicitudRepository
            .Setup(r => r.GetByIdWithDetallesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solicitud);
        _materialRepository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(material);
        _solicitudRepository
            .Setup(r => r.AddEntregaAsync(It.IsAny<EntregaMaterial>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().ResolveSolicitudAsync(
            20,
            [new ResolveDetalleDto(1, 2, MaterialId: 5)],
            bodegueroId: 9);

        Assert.True(result.Success, result.Message);
        Assert.Equal(5, solicitud.Detalles.Single().MaterialId);
        Assert.Equal(98m, material.Stock);
        Assert.Equal(SolicitudMaterialEstado.AprobadaTotal, solicitud.Estado);
    }
}
