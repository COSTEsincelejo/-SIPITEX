using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Application.Services;

public class FichaService : IFichaService
{
    private readonly IFichaRepository _fichaRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IProductionOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;

    public FichaService(
        IFichaRepository fichaRepository,
        IProductionOrderRepository orderRepository,
        IProductionOrderService orderService,
        IUnitOfWork unitOfWork)
    {
        _fichaRepository = fichaRepository;
        _orderRepository = orderRepository;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<FichaDto>> GetFichasAsync(CancellationToken cancellationToken = default)
    {
        var fichas = await _fichaRepository.GetAllAsync(cancellationToken);
        return fichas.Select(f => new FichaDto(
            f.Id,
            f.FichaCode,
            f.ProcessName,
            f.InstructorName,
            f.ProductionOrder?.OrderNumber)).ToList();
    }

    public async Task<ServiceResult> RegisterSessionAsync(RegisterProductionDto dto, CancellationToken cancellationToken = default)
    {
        var ficha = await _fichaRepository.GetByIdAsync(dto.FichaId, cancellationToken);
        if (ficha is null) return ServiceResult.Fail("Ficha no encontrada.");

        ficha.ProductionOrderId = dto.ProductionOrderId;
        _fichaRepository.Update(ficha);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _orderService.RegisterProductionAsync(dto.ProductionOrderId, dto.Units, cancellationToken);
    }

    public async Task<ServiceResult> QuickRegisterAsync(int fichaId, int units, CancellationToken cancellationToken = default)
    {
        if (units <= 0) return ServiceResult.Fail("Ingrese una cantidad válida.");

        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha?.ProductionOrderId is null)
            return ServiceResult.Fail("Ficha sin orden asignada.");

        return await _orderService.RegisterProductionAsync(ficha.ProductionOrderId.Value, units, cancellationToken);
    }
}
