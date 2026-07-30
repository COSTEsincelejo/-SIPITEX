using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Inspecciones de calidad ligadas a órdenes de producción
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

    // Lista de inspecciones, las más recientes primero
    public async Task<IReadOnlyList<QualityRecordDto>> GetRecordsAsync(CancellationToken cancellationToken = default)
    {
        // Con esto traigo todas las inspecciones de BD
        var records = await _qualityRepository.GetAllAsync(cancellationToken);
        // Ordeno por fecha descendente y mapeo a DTO
        return records
            .OrderByDescending(r => r.InspectionDate)
            .Select(r => new QualityRecordDto(
                r.ProductionOrder.OrderNumber,
                r.UnitsInspected,
                r.Result,
                r.InspectionDate,
                r.MotivoReproceso,
                r.Responsable))
            .ToList();
    }

    // Guarda una inspección; si es reproceso pide motivo y responsable
    public async Task<ServiceResult> AddRecordAsync(CreateQualityRecordDto dto, CancellationToken cancellationToken = default)
    {
        // Acá reviso que la orden exista y las unidades sean válidas
        var order = await _orderRepository.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null || dto.Units <= 0)
            return ServiceResult.Fail("Datos incompletos.");

        // Reproceso obliga motivo y responsable
        if (dto.Result == QualityResult.Reproceso)
        {
            // Sin motivo o responsable no guardo
            if (string.IsNullOrWhiteSpace(dto.MotivoReproceso) || string.IsNullOrWhiteSpace(dto.Responsable))
                return ServiceResult.Fail("Para reproceso indique motivo y responsable.");
        }

        // Armo la entidad con la fecha de hoy
        await _qualityRepository.AddAsync(new QualityRecord
        {
            ProductionOrderId = dto.ProductionOrderId,
            UnitsInspected = dto.Units,
            Result = dto.Result,
            // Solo guardo motivo/responsable si aplica reproceso
            MotivoReproceso = dto.Result == QualityResult.Reproceso ? dto.MotivoReproceso?.Trim() : null,
            Responsable = dto.Result == QualityResult.Reproceso ? dto.Responsable?.Trim() : null,
            InspectionDate = DateOnly.FromDateTime(DateTime.Today)
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Inspección registrada.");
    }
}
