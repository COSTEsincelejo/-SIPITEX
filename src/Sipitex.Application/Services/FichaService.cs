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

    public async Task<IReadOnlyList<FichaDto>> GetFichasAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        var fichas = await _fichaRepository.GetAllAsync(cancellationToken);
        if (IsInstructorViewer(viewerRole, viewerUserId))
        {
            fichas = fichas
                .Where(f => BelongsToInstructor(f, viewerUserId!.Value, viewerName))
                .ToList();
        }

        return fichas.Select(f => new FichaDto(
            f.Id,
            f.FichaCode,
            f.ProcessName,
            f.InstructorName,
            f.ProductionOrder?.OrderNumber,
            f.InstructorUserId)).ToList();
    }

    public async Task<IReadOnlyList<ProductionSessionDto>> GetRecentSessionsAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        // Pedimos más filas si hay que filtrar, para no quedarnos cortos tras el scope.
        var take = IsInstructorViewer(viewerRole, viewerUserId) ? 100 : 20;
        var sessions = await _sessionRepository.GetRecentAsync(take, cancellationToken);

        if (IsInstructorViewer(viewerRole, viewerUserId))
        {
            sessions = sessions
                .Where(s =>
                    s.RegisteredByUserId == viewerUserId
                    || BelongsToInstructor(s.Ficha, viewerUserId!.Value, viewerName))
                .Take(20)
                .ToList();
        }

        return sessions.Select(s => new ProductionSessionDto(
            s.Id,
            s.Ficha.FichaCode,
            s.ProductionOrder.OrderNumber,
            s.Units,
            s.Observations,
            s.SessionDate,
            s.Ficha.InstructorName,
            s.RegisteredByUserId)).ToList();
    }

    public async Task<ServiceResult> RegisterSessionAsync(
        RegisterProductionDto dto,
        int? registeredByUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        if (dto.Units <= 0) return ServiceResult.Fail("Ingrese una cantidad válida.");

        var ficha = await _fichaRepository.GetByIdAsync(dto.FichaId, cancellationToken);
        if (ficha is null) return ServiceResult.Fail("Ficha no encontrada.");

        if (IsInstructorViewer(viewerRole, registeredByUserId)
            && !BelongsToInstructor(ficha, registeredByUserId!.Value, viewerName))
        {
            return ServiceResult.Fail("Solo puede registrar producción en sus propias fichas.");
        }

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
            SessionDate = DateTime.Now,
            RegisteredByUserId = registeredByUserId
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var production = await _orderService.RegisterProductionAsync(dto.ProductionOrderId, dto.Units, cancellationToken);
        return production.Success
            ? ServiceResult.Ok("Sesión diaria registrada.")
            : production;
    }

    public async Task<ServiceResult> QuickRegisterAsync(
        int fichaId,
        int units,
        string? observations = null,
        int? registeredByUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        if (units <= 0) return ServiceResult.Fail("Ingrese una cantidad válida.");

        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha?.ProductionOrderId is null)
            return ServiceResult.Fail("Ficha sin orden asignada.");

        return await RegisterSessionAsync(
            new RegisterProductionDto(ficha.ProductionOrderId.Value, fichaId, units, observations),
            registeredByUserId,
            viewerRole,
            viewerName,
            cancellationToken);
    }

    public async Task<ServiceResult> CreateFichaAsync(
        CreateFichaDto dto,
        int? instructorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var code = (dto.FichaCode ?? string.Empty).Trim();
        var process = (dto.ProcessName ?? string.Empty).Trim();
        var instructor = (dto.InstructorName ?? string.Empty).Trim();
        var turno = (dto.Turno ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code))
            return ServiceResult.Fail("El código de ficha es obligatorio.");
        if (string.IsNullOrWhiteSpace(process))
            return ServiceResult.Fail("El proceso es obligatorio.");
        if (string.IsNullOrWhiteSpace(instructor))
            return ServiceResult.Fail("El instructor es obligatorio.");
        if (string.IsNullOrWhiteSpace(turno))
            return ServiceResult.Fail("El turno es obligatorio.");
        if (code.Length > 30)
            return ServiceResult.Fail("El código de ficha no puede superar 30 caracteres.");
        if (turno.Length > 20)
            return ServiceResult.Fail("El turno no puede superar 20 caracteres.");

        if (await _fichaRepository.ExistsByCodeAsync(code, cancellationToken))
            return ServiceResult.Fail("Ya existe una ficha con ese código.");

        if (dto.ProductionOrderId is int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
                return ServiceResult.Fail("Orden de producción no encontrada.");
        }

        await _fichaRepository.AddAsync(new Ficha
        {
            FichaCode = code,
            ProcessName = process,
            InstructorName = instructor,
            Turno = turno,
            ProductionOrderId = dto.ProductionOrderId is > 0 ? dto.ProductionOrderId : null,
            InstructorUserId = instructorUserId is > 0 ? instructorUserId : null
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Ficha {code} registrada.");
    }

    private static bool IsInstructorViewer(string? viewerRole, int? viewerUserId) =>
        viewerUserId is > 0
        && string.Equals(viewerRole, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);

    private static bool BelongsToInstructor(Ficha ficha, int instructorUserId, string? instructorName)
    {
        if (ficha.InstructorUserId == instructorUserId)
            return true;

        // Compatibilidad con fichas legacy solo por nombre (sin FK aún).
        return ficha.InstructorUserId is null
               && !string.IsNullOrWhiteSpace(instructorName)
               && string.Equals(ficha.InstructorName, instructorName, StringComparison.OrdinalIgnoreCase);
    }
}
