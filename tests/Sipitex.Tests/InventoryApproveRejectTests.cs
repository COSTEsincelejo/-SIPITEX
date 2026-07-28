using Moq;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Tests;

public class InventoryApproveRejectTests
{
    private readonly Mock<IMaterialRepository> _materialRepository = new();
    private readonly Mock<IMaterialRequestRepository> _requestRepository = new();
    private readonly Mock<IProductionOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private InventoryService CreateSut() => new(
        _materialRepository.Object,
        _requestRepository.Object,
        _orderRepository.Object,
        _unitOfWork.Object);

    private static MaterialRequest CreatePendingRequest(decimal stock, decimal quantity)
    {
        var material = new Material
        {
            Id = 1,
            Name = "Tela",
            Stock = stock,
            Unit = MaterialUnit.Metros
        };

        return new MaterialRequest
        {
            Id = 10,
            MaterialId = 1,
            Material = material,
            Quantity = quantity,
            ProductionOrderId = 1,
            Status = RequestStatus.Pendiente
        };
    }

    [Fact]
    public async Task ApproveRequestAsync_WhenStockIsEnough_DeductsStockAndApproves()
    {
        var request = CreatePendingRequest(stock: 50, quantity: 12);
        _requestRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await CreateSut().ApproveRequestAsync(10);

        Assert.True(result.Success);
        Assert.Equal(38m, request.Material.Stock);
        Assert.Equal(RequestStatus.Aprobada, request.Status);
        _materialRepository.Verify(r => r.Update(request.Material), Times.Once);
        _requestRepository.Verify(r => r.Update(request), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectRequestAsync_DoesNotChangeStock()
    {
        var request = CreatePendingRequest(stock: 50, quantity: 12);
        _requestRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await CreateSut().RejectRequestAsync(10);

        Assert.True(result.Success);
        Assert.Equal(50m, request.Material.Stock);
        Assert.Equal(RequestStatus.Rechazada, request.Status);
        _materialRepository.Verify(r => r.Update(It.IsAny<Material>()), Times.Never);
        _requestRepository.Verify(r => r.Update(request), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveRequestAsync_WhenStockIsInsufficient_FailsWithoutChangingStock()
    {
        var request = CreatePendingRequest(stock: 5, quantity: 12);
        _requestRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await CreateSut().ApproveRequestAsync(10);

        Assert.False(result.Success);
        Assert.Equal(5m, request.Material.Stock);
        Assert.Equal(RequestStatus.Pendiente, request.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
