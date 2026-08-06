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
    private readonly IUserRepository _userRepository;
    private readonly IProductionOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;

    public FichaService(
        IFichaRepository fichaRepository,
        IProductionOrderRepository orderRepository,
        IProductionSessionRepository sessionRepository,
        IUserRepository userRepository,
        IProductionOrderService orderService,
        IUnitOfWork unitOfWork)
    {
        _fichaRepository = fichaRepository;
        _orderRepository = orderRepository;
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
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

        return fichas.Select(MapFicha).ToList();
    }

    public async Task<IReadOnlyList<InstructorOptionDto>> GetActiveInstructorsAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users
            .Where(u => u.IsActive
                        && string.Equals(u.Rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.Nombre)
            .Select(u => new InstructorOptionDto(u.Id, u.Nombre))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductionSessionDto>> GetRecentSessionsAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default)
    {
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
            FormatInstructorNames(s.Ficha),
            s.RegisteredByUserId,
            s.Ficha.Turno)).ToList();
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
        CancellationToken cancellationToken = default)
    {
        var code = (dto.FichaCode ?? string.Empty).Trim();
        var process = (dto.ProcessName ?? string.Empty).Trim();
        var turno = (dto.Turno ?? string.Empty).Trim();
        var orderText = string.IsNullOrWhiteSpace(dto.AssignedOrderText)
            ? null
            : dto.AssignedOrderText.Trim();
        var orderId = dto.ProductionOrderId is > 0 ? dto.ProductionOrderId : null;
        var instructorIds = (dto.InstructorUserIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (string.IsNullOrWhiteSpace(code))
            return ServiceResult.Fail("El código de ficha es obligatorio.");
        if (string.IsNullOrWhiteSpace(process))
            return ServiceResult.Fail("El proceso es obligatorio.");
        if (instructorIds.Count == 0)
            return ServiceResult.Fail("Debe asignar al menos un instructor registrado.");
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

        if (await _fichaRepository.ExistsByCodeAsync(code, cancellationToken))
            return ServiceResult.Fail("Ya existe una ficha con ese código.");

        // Si mandan orden existente, verifico que exista
        if (orderId is int existingOrderId)
        {
            var order = await _orderRepository.GetByIdAsync(existingOrderId, cancellationToken);
            if (order is null)
                return ServiceResult.Fail("Orden de producción no encontrada.");
        }

        var instructors = new List<User>();
        foreach (var userId in instructorIds)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            var validation = ValidateInstructorUser(user);
            if (validation is not null)
                return validation;
            instructors.Add(user!);
        }

        var ficha = new Ficha
        {
            FichaCode = code,
            ProcessName = process,
            Turno = turno,
            ProductionOrderId = orderId,
            AssignedOrderText = orderText
        };

        foreach (var user in instructors)
        {
            ficha.Instructors.Add(new FichaInstructor
            {
                UserId = user.Id,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        SyncPrimaryInstructorFields(ficha, instructors);

        await _fichaRepository.AddAsync(ficha, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Ficha {code} registrada.");
    }

    public async Task<ServiceResult> AssignInstructorAsync(
        int fichaId,
        int instructorUserId,
        int? actorUserId = null,
        string? actorRole = null,
        string? actorName = null,
        string? proceso = null,
        CancellationToken cancellationToken = default)
    {
        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha is null) return ServiceResult.Fail("Ficha no encontrada.");

        if (!CanManageInstructors(ficha, actorUserId, actorRole, actorName))
            return ServiceResult.Fail("No tiene permiso para asignar instructores en esta ficha.");

        var user = await _userRepository.GetByIdAsync(instructorUserId, cancellationToken);
        var validation = ValidateInstructorUser(user);
        if (validation is not null)
            return validation;

        if (ficha.Instructors.Any(i => i.UserId == instructorUserId))
            return ServiceResult.Fail("Ese instructor ya está asignado a la ficha.");

        var procesoNorm = NormalizeProceso(proceso);
        if (procesoNorm is { Length: > 60 })
            return ServiceResult.Fail("El proceso no puede superar 60 caracteres.");

        ficha.Instructors.Add(new FichaInstructor
        {
            FichaId = ficha.Id,
            UserId = instructorUserId,
            User = user!,
            AssignedAtUtc = DateTime.UtcNow,
            Proceso = procesoNorm
        });

        SyncPrimaryInstructorFields(ficha);
        _fichaRepository.Update(ficha);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"{user!.Nombre} asignado a la ficha {ficha.FichaCode}.");
    }

    public async Task<ServiceResult> UpdateInstructorProcesoAsync(
        int fichaId,
        int instructorUserId,
        string? proceso,
        int? actorUserId = null,
        string? actorRole = null,
        string? actorName = null,
        CancellationToken cancellationToken = default)
    {
        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha is null) return ServiceResult.Fail("Ficha no encontrada.");

        var assignment = ficha.Instructors.FirstOrDefault(i => i.UserId == instructorUserId);
        if (assignment is null)
            return ServiceResult.Fail("Ese instructor no está asignado a la ficha.");

        // Admin: cualquiera. Instructor: solo su propia asignación (y debe pertenecer a la ficha).
        if (!CanEditInstructorProceso(ficha, instructorUserId, actorUserId, actorRole, actorName))
            return ServiceResult.Fail("No tiene permiso para editar el proceso de este instructor.");

        var procesoNorm = NormalizeProceso(proceso);
        if (procesoNorm is { Length: > 60 })
            return ServiceResult.Fail("El proceso no puede superar 60 caracteres.");

        assignment.Proceso = procesoNorm;
        _fichaRepository.Update(ficha);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Proceso actualizado.");
    }

    public async Task<ServiceResult> RemoveInstructorAsync(
        int fichaId,
        int instructorUserId,
        int? actorUserId = null,
        string? actorRole = null,
        string? actorName = null,
        CancellationToken cancellationToken = default)
    {
        var ficha = await _fichaRepository.GetByIdAsync(fichaId, cancellationToken);
        if (ficha is null) return ServiceResult.Fail("Ficha no encontrada.");

        if (!CanManageInstructors(ficha, actorUserId, actorRole, actorName))
            return ServiceResult.Fail("No tiene permiso para quitar instructores de esta ficha.");

        var assignment = ficha.Instructors.FirstOrDefault(i => i.UserId == instructorUserId);
        if (assignment is null)
            return ServiceResult.Fail("Ese instructor no está asignado a la ficha.");

        if (ficha.Instructors.Count <= 1)
            return ServiceResult.Fail("La ficha debe conservar al menos un instructor.");

        ficha.Instructors.Remove(assignment);
        SyncPrimaryInstructorFields(ficha);
        _fichaRepository.Update(ficha);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Instructor quitado de la ficha.");
    }

    private static FichaDto MapFicha(Ficha f)
    {
        var instructors = f.Instructors
            .OrderBy(i => i.User?.Nombre ?? string.Empty)
            .Select(i => new FichaInstructorDto(i.UserId, i.User?.Nombre ?? string.Empty, i.Proceso))
            .Where(i => !string.IsNullOrWhiteSpace(i.Nombre))
            .ToList();

        return new FichaDto(
            f.Id,
            f.FichaCode,
            f.ProcessName,
            FormatInstructorNames(f),
            f.ProductionOrder?.OrderNumber ?? f.AssignedOrderText,
            f.InstructorUserId,
            f.Turno,
            instructors);
    }

    private static string FormatInstructorNames(Ficha ficha)
    {
        var names = ficha.Instructors
            .Select(i => i.User?.Nombre)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        if (names.Count > 0)
            return string.Join(", ", names!);

        return ficha.InstructorName ?? string.Empty;
    }

    private static void SyncPrimaryInstructorFields(Ficha ficha, IReadOnlyList<User>? knownUsers = null)
    {
        var ordered = ficha.Instructors
            .OrderBy(i => i.AssignedAtUtc)
            .ThenBy(i => i.UserId)
            .ToList();

        if (ordered.Count == 0)
        {
            ficha.InstructorUserId = null;
            ficha.InstructorName = string.Empty;
            return;
        }

        ficha.InstructorUserId = ordered[0].UserId;

        var names = ordered
            .Select(i =>
            {
                if (!string.IsNullOrWhiteSpace(i.User?.Nombre))
                    return i.User!.Nombre;
                return knownUsers?.FirstOrDefault(u => u.Id == i.UserId)?.Nombre;
            })
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        ficha.InstructorName = string.Join(", ", names!);
    }

    private static ServiceResult? ValidateInstructorUser(User? user)
    {
        if (user is null)
            return ServiceResult.Fail("El instructor seleccionado no existe.");
        if (!user.IsActive)
            return ServiceResult.Fail("El instructor seleccionado está inactivo.");
        if (!string.Equals(user.Rol, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase))
            return ServiceResult.Fail("Solo se pueden asignar usuarios con rol Instructor.");
        return null;
    }

    private static bool CanManageInstructors(
        Ficha ficha,
        int? actorUserId,
        string? actorRole,
        string? actorName)
    {
        if (string.Equals(actorRole, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return true;

        return IsInstructorViewer(actorRole, actorUserId)
               && BelongsToInstructor(ficha, actorUserId!.Value, actorName);
    }

    // Editar proceso: Admin cualquiera; Instructor solo su propio UserId en la ficha (vía BelongsToInstructor)
    private static bool CanEditInstructorProceso(
        Ficha ficha,
        int targetInstructorUserId,
        int? actorUserId,
        string? actorRole,
        string? actorName)
    {
        if (string.Equals(actorRole, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            return true;

        return IsInstructorViewer(actorRole, actorUserId)
               && actorUserId == targetInstructorUserId
               && BelongsToInstructor(ficha, actorUserId!.Value, actorName);
    }

    private static string? NormalizeProceso(string? proceso) =>
        string.IsNullOrWhiteSpace(proceso) ? null : proceso.Trim();

    private static bool IsInstructorViewer(string? viewerRole, int? viewerUserId) =>
        viewerUserId is > 0
        && string.Equals(viewerRole, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);

    private static bool BelongsToInstructor(Ficha ficha, int instructorUserId, string? instructorName)
    {
        if (ficha.Instructors.Any(i => i.UserId == instructorUserId))
            return true;

        if (ficha.InstructorUserId == instructorUserId)
            return true;

        // Compatibilidad con fichas legacy solo por nombre (sin FK ni M2M aún)
        return ficha.InstructorUserId is null
               && ficha.Instructors.Count == 0
               && !string.IsNullOrWhiteSpace(instructorName)
               && string.Equals(ficha.InstructorName, instructorName, StringComparison.OrdinalIgnoreCase);
    }
}
