using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IMaterialRepository _materialRepository;
    private readonly IMaterialRequestRepository _requestRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IMaterialRepository materialRepository,
        IMaterialRequestRepository requestRepository,
        IProductionOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _materialRepository = materialRepository;
        _requestRepository = requestRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(CancellationToken cancellationToken = default)
    {
        var materials = await _materialRepository.GetAllAsync(cancellationToken);
        return materials.Select(MapMaterial).ToList();
    }

    public async Task<ServiceResult> AddMaterialAsync(CreateMaterialDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Stock < 0)
            return ServiceResult.Fail("Ingrese nombre y stock válidos.");

        var material = new Material
        {
            Code = $"mat{DateTime.UtcNow.Ticks}",
            Name = dto.Name.Trim(),
            Unit = dto.Unit,
            Stock = dto.Stock,
            MinStock = 10,
            Status = MaterialStatus.Bueno
        };

        await _materialRepository.AddAsync(material, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AdjustStockAsync(AdjustStockDto dto, CancellationToken cancellationToken = default)
    {
        var material = await _materialRepository.GetByIdAsync(dto.MaterialId, cancellationToken);
        if (material is null) return ServiceResult.Fail("Material no encontrado.");

        material.Stock = Math.Max(0, dto.NewStock);
        _materialRepository.Update(material);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<MaterialRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _requestRepository.GetAllAsync(cancellationToken);
        return requests.Select(r => new MaterialRequestDto(
            r.Id,
            r.Material.Name,
            r.Quantity,
            r.ProductionOrder.OrderNumber,
            r.Status)).ToList();
    }

    public async Task<ServiceResult> CreateRequestAsync(CreateMaterialRequestDto dto, CancellationToken cancellationToken = default)
    {
        var material = await _materialRepository.GetByIdAsync(dto.MaterialId, cancellationToken);
        var order = await _orderRepository.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (material is null || order is null || dto.Quantity <= 0)
            return ServiceResult.Fail("Datos inválidos.");

        await _requestRepository.AddAsync(new MaterialRequest
        {
            MaterialId = dto.MaterialId,
            ProductionOrderId = dto.ProductionOrderId,
            Quantity = dto.Quantity,
            Status = RequestStatus.Pendiente
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ApproveRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null || request.Status != RequestStatus.Pendiente)
            return ServiceResult.Fail("Solicitud no válida.");

        if (request.Material.Stock < request.Quantity)
            return ServiceResult.Fail("Stock insuficiente para aprobar solicitud.");

        request.Material.Stock -= request.Quantity;
        request.Status = RequestStatus.Aprobada;
        _materialRepository.Update(request.Material);
        _requestRepository.Update(request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    private static MaterialDto MapMaterial(Material m) => new(
        m.Id,
        m.Name,
        UnitHelper.ToDisplay(m.Unit),
        m.Stock,
        m.Status,
        m.MinStock,
        m.Stock < m.MinStock);
}
