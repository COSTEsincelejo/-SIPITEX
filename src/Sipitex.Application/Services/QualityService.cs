using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class QualityService : IQualityService
{
    private readonly IQualityRepository _qualityRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public QualityService(
        IQualityRepository qualityRepository,
        IProductionOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _qualityRepository = qualityRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<QualityRecordDto>> GetRecordsAsync(CancellationToken cancellationToken = default)
    {
        var records = await _qualityRepository.GetAllAsync(cancellationToken);
        return records
            .OrderByDescending(r => r.InspectionDate)
            .Select(r => new QualityRecordDto(
                r.ProductionOrder.OrderNumber,
                r.UnitsInspected,
                r.Result,
                r.InspectionDate))
            .ToList();
    }

    public async Task<ServiceResult> AddRecordAsync(CreateQualityRecordDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null || dto.Units <= 0)
            return ServiceResult.Fail("Datos incompletos.");

        await _qualityRepository.AddAsync(new QualityRecord
        {
            ProductionOrderId = dto.ProductionOrderId,
            UnitsInspected = dto.Units,
            Result = dto.Result,
            InspectionDate = DateOnly.FromDateTime(DateTime.Today)
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }
}
