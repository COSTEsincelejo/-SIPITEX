using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

public class FichaService : IFichaService
{
    private readonly IFichaRepository _fichaRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IProductionSessionRepository _sessionRepository;
    private readonly IProductionOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;

    public FichaService(
        IFichaRepository fichaRepository,
        IProductionOrderRepository orderRepository,
        IProductionSessionRepository sessionRepository,
        IProductionOrderService orderService,
        IUnitOfWork unitOfWork)
    {
        _fichaRepository = fichaRepository;
        _orderRepository = orderRepository;
        _sessionRepository = sessionRepository;
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

    public async Task<IReadOnlyList<ProductionSessionDto>> GetRecentSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetRecentAsync(20, cancellationToken);
        return sessions.Select(s => new ProductionSessionDto(
            s.Id,
            s.Ficha.FichaCode,
            s.ProductionOrder.OrderNumber,
            s.Units,
            s.Observations,
            s.SessionDate)).ToList();
    }

    public async Task<ServiceResult> RegisterSessionAsync(RegisterProductionDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Units <= 0) return ServiceResult.Fail("Ingrese una cantidad válida.");

        var ficha = await _fichaRepository.GetByIdAsync(dto.FichaId, cancellationToken);
        if (ficha is null) return ServiceResult.Fail("Ficha no encontrada.");

        var order = await _orderRepository.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null) return ServiceResult.Fail("Orden no encontrada.");

        ficha.ProductionOrderId = dto.ProductionOrderId;
        _fichaRepository.Update(ficha);

        await _sessionRepository.AddAsync(new ProductionSession
        {
            FichaId = dto.FichaId,
            ProductionOrderId = dto.ProductionOrderId,
            Units = dto.Units,
            Observations = dto.Observations?.Trim() ?? string.Empty,
            SessionDate = DateTime.Now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var production = await _orderService.RegisterProductionAsync(dto.ProductionOrderId, dto.Units, cancellationToken);
        return production.Success
            ? ServiceResult.Ok("Sesión diaria registrada.")
            : production;
    }

    public async Task<ServiceResult> QuickRegisterAsync(int fichaId, int units, string? observations = null, CancellationToken cancellationToken = default)
    {
        if (units <= 0) return ServiceResult.Fail("Ingrese una cantidad válida.");

        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha?.ProductionOrderId is null)
            return ServiceResult.Fail("Ficha sin orden asignada.");

        return await RegisterSessionAsync(
            new RegisterProductionDto(ficha.ProductionOrderId.Value, fichaId, units, observations),
            cancellationToken);
    }
}
