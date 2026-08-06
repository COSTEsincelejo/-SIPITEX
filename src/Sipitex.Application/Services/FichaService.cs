using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Fichas de producción y registro de sesiones diarias por el instructor
public class FichaService : IFichaService
{
    private readonly IFichaRepository _fichaRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IProductionSessionRepository _sessionRepository;
    // Para registrar avance y consumo de materiales en la orden
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

    // Lista fichas; si quien mira es instructor solo ve las suyas
    public async Task<IReadOnlyList<FichaDto>> GetFichasAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        // Traigo todas las fichas de BD
        var fichas = await _fichaRepository.GetAllAsync(cancellationToken);
        // Acá reviso si el que consulta es instructor
        if (IsInstructorViewer(viewerRole, viewerUserId))
        {
            // Solo dejo las fichas que le pertenecen
            fichas = fichas
                .Where(f => BelongsToInstructor(f, viewerUserId!.Value, viewerName))
                .ToList();
        }

        // Mapeo a DTO para la vista (orden real o texto manual)
        return fichas.Select(f => new FichaDto(
            f.Id,
            f.FichaCode,
            f.ProcessName,
            f.InstructorName,
            f.ProductionOrder?.OrderNumber ?? f.AssignedOrderText,
            f.InstructorUserId,
            f.Turno)).ToList();
    }

    // Sesiones recientes de producción (también filtradas por instructor si aplica)
    public async Task<IReadOnlyList<ProductionSessionDto>> GetRecentSessionsAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        // Pedimos más filas si hay que filtrar, para no quedarnos cortos tras el scope.
        var take = IsInstructorViewer(viewerRole, viewerUserId) ? 100 : 20;
        var sessions = await _sessionRepository.GetRecentAsync(take, cancellationToken);

        // Si es instructor, filtro por sus fichas o lo que él registró
        if (IsInstructorViewer(viewerRole, viewerUserId))
        {
            sessions = sessions
                .Where(s =>
                    s.RegisteredByUserId == viewerUserId
                    || BelongsToInstructor(s.Ficha, viewerUserId!.Value, viewerName))
                .Take(20)
                .ToList();
        }

        // Proyecto cada sesión al DTO con datos de ficha y orden
        return sessions.Select(s => new ProductionSessionDto(
            s.Id,
            s.Ficha.FichaCode,
            s.ProductionOrder.OrderNumber,
            s.Units,
            s.Observations,
            s.SessionDate,
            s.Ficha.InstructorName,
            s.RegisteredByUserId,
            s.Ficha.Turno)).ToList();
    }

    // Registra una sesión de producción y actualiza el avance de la orden
    public async Task<ServiceResult> RegisterSessionAsync(
        RegisterProductionDto dto,
        int? registeredByUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
        // Validación básica de cantidad
        if (dto.Units <= 0) return ServiceResult.Fail("Ingrese una cantidad válida.");

        // Busco la ficha en BD
        var ficha = await _fichaRepository.GetByIdAsync(dto.FichaId, cancellationToken);
        if (ficha is null) return ServiceResult.Fail("Ficha no encontrada.");

        // Instructor solo puede registrar en sus propias fichas
        if (IsInstructorViewer(viewerRole, registeredByUserId)
            && !BelongsToInstructor(ficha, registeredByUserId!.Value, viewerName))
        {
            return ServiceResult.Fail("Solo puede registrar producción en sus propias fichas.");
        }

        // Verifico que la orden exista
        var order = await _orderRepository.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null) return ServiceResult.Fail("Orden no encontrada.");

        // Actualizo la orden asignada a la ficha por si cambió
        ficha.ProductionOrderId = dto.ProductionOrderId;
        _fichaRepository.Update(ficha);

        // Creo el registro de sesión diaria
        await _sessionRepository.AddAsync(new ProductionSession
        {
            FichaId = dto.FichaId,
            ProductionOrderId = dto.ProductionOrderId,
            Units = dto.Units,
            Observations = dto.Observations?.Trim() ?? string.Empty,
            SessionDate = DateTime.Now,
            RegisteredByUserId = registeredByUserId
        }, cancellationToken);

        // Guardo ficha actualizada y sesión nueva
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Esto también descuenta materiales según el BOM
        var production = await _orderService.RegisterProductionAsync(dto.ProductionOrderId, dto.Units, cancellationToken);
        // Si el consumo falló, devuelvo ese error; si no, mensaje de éxito
        return production.Success
            ? ServiceResult.Ok("Sesión diaria registrada.")
            : production;
    }

    // Atajo: registra usando la orden que ya tiene la ficha
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
        // La ficha tiene que tener orden asignada
        if (ficha?.ProductionOrderId is null)
            return ServiceResult.Fail("Ficha sin orden asignada.");

        // Reutilizo RegisterSessionAsync con los datos de la ficha
        return await RegisterSessionAsync(
            new RegisterProductionDto(ficha.ProductionOrderId.Value, fichaId, units, observations),
            registeredByUserId,
            viewerRole,
            viewerName,
            cancellationToken);
    }

    // Alta de una ficha nueva (admin o quien tenga permiso)
    public async Task<ServiceResult> CreateFichaAsync(
        CreateFichaDto dto,
        int? instructorUserId = null,
        CancellationToken cancellationToken = default)
    {
        // Limpio espacios de los campos de texto
        var code = (dto.FichaCode ?? string.Empty).Trim();
        var process = (dto.ProcessName ?? string.Empty).Trim();
        var instructor = (dto.InstructorName ?? string.Empty).Trim();
        var turno = (dto.Turno ?? string.Empty).Trim();
        var orderText = string.IsNullOrWhiteSpace(dto.AssignedOrderText)
            ? null
            : dto.AssignedOrderText.Trim();
        var orderId = dto.ProductionOrderId is > 0 ? dto.ProductionOrderId : null;

        // Validaciones campo por campo
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
        if (orderText is { Length: > 100 })
            return ServiceResult.Fail("La orden manual no puede superar 100 caracteres.");

        // Mutuamente excluyentes: FK de orden o texto manual, nunca ambos
        if (orderId is not null && orderText is not null)
            return ServiceResult.Fail("No puedes seleccionar una orden y escribir una manual al mismo tiempo");

        // No puede repetirse el código
        if (await _fichaRepository.ExistsByCodeAsync(code, cancellationToken))
            return ServiceResult.Fail("Ya existe una ficha con ese código.");

        // Si mandan orden existente, verifico que exista
        if (orderId is int existingOrderId)
        {
            var order = await _orderRepository.GetByIdAsync(existingOrderId, cancellationToken);
            if (order is null)
                return ServiceResult.Fail("Orden de producción no encontrada.");
        }

        // Inserto la ficha nueva
        await _fichaRepository.AddAsync(new Ficha
        {
            FichaCode = code,
            ProcessName = process,
            InstructorName = instructor,
            Turno = turno,
            ProductionOrderId = orderId,
            AssignedOrderText = orderText,
            InstructorUserId = instructorUserId is > 0 ? instructorUserId : null
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Ficha {code} registrada.");
    }

    // Acá reviso si quien mira es instructor con id válido
    private static bool IsInstructorViewer(string? viewerRole, int? viewerUserId) =>
        viewerUserId is > 0
        && string.Equals(viewerRole, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);

    // La ficha le pertenece al instructor por userId o por nombre (legacy)
    private static bool BelongsToInstructor(Ficha ficha, int instructorUserId, string? instructorName)
    {
        // Caso normal: la ficha tiene FK al usuario instructor
        if (ficha.InstructorUserId == instructorUserId)
            return true;

        // Compatibilidad con fichas legacy solo por nombre (sin FK aún).
        return ficha.InstructorUserId is null
               && !string.IsNullOrWhiteSpace(instructorName)
               && string.Equals(ficha.InstructorName, instructorName, StringComparison.OrdinalIgnoreCase);
    }
}
