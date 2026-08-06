using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;

namespace Sipitex.Application.Services;

// Actas de verificación: observación + checklist + firma del instructor de la ficha
public class ActaVerificacionService : IActaVerificacionService
{
    private readonly IActaVerificacionRepository _actaRepository;
    private readonly IProductionOrderRepository _orderRepository;
    private readonly IFichaRepository _fichaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActaVerificacionService(
        IActaVerificacionRepository actaRepository,
        IProductionOrderRepository orderRepository,
        IFichaRepository fichaRepository,
        IUnitOfWork unitOfWork)
    {
        _actaRepository = actaRepository;
        _orderRepository = orderRepository;
        _fichaRepository = fichaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ActaVerificacionDto>> GetActasAsync(
        int? viewerUserId,
        string? viewerRole,
        string? viewerName,
        CancellationToken cancellationToken = default)
    {
        var actas = await _actaRepository.GetAllAsync(cancellationToken);
        var isAdmin = IsAdmin(viewerRole);
        var isInstructor = IsInstructorViewer(viewerRole, viewerUserId);

        IEnumerable<ActaVerificacion> filtered = actas;
        if (!isAdmin && isInstructor)
        {
            filtered = actas.Where(a =>
                BelongsToInstructor(a.Ficha, viewerUserId!.Value, viewerName)
                || a.InstructorId == viewerUserId);
        }

        return filtered.Select(a => ToDto(a, viewerUserId, viewerRole)).ToList();
    }

    public async Task<ActaVerificacionDto?> GetByIdAsync(
        int id,
        int? viewerUserId,
        string? viewerRole,
        string? viewerName,
        CancellationToken cancellationToken = default)
    {
        var acta = await _actaRepository.GetByIdAsync(id, cancellationToken);
        if (acta is null) return null;

        if (!CanView(acta, viewerUserId, viewerRole, viewerName))
            return null;

        return ToDto(acta, viewerUserId, viewerRole);
    }

    public async Task<ServiceResult> CreateAsync(
        GuardarActaVerificacionDto dto,
        int actorUserId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default)
    {
        if (IsAdmin(actorRole))
            return ServiceResult.Fail("El administrador puede consultar actas, pero no crearlas ni firmarlas en nombre del instructor.");

        if (!IsInstructorViewer(actorRole, actorUserId))
            return ServiceResult.Fail("Solo un instructor asignado puede registrar el acta.");

        var validation = ValidateChecklistAndObservacion(dto);
        if (validation is not null) return validation;

        var order = await _orderRepository.GetByIdAsync(dto.ProductionOrderId, cancellationToken);
        if (order is null)
            return ServiceResult.Fail("La orden de producción no existe.");

        var ficha = await _fichaRepository.GetByIdAsync(dto.FichaId, cancellationToken);
        if (ficha is null)
            return ServiceResult.Fail("La ficha no existe.");

        if (!BelongsToInstructor(ficha, actorUserId, actorName))
            return ServiceResult.Fail("Solo puede actuar sobre sus propias fichas.");

        var acta = new ActaVerificacion
        {
            ProductionOrderId = dto.ProductionOrderId,
            FichaId = dto.FichaId,
            InstructorId = actorUserId,
            Observacion = dto.Observacion.Trim(),
            CumpleEspecificaciones = dto.CumpleEspecificaciones,
            CumpleAcabados = dto.CumpleAcabados,
            CumpleSinDefectos = dto.CumpleSinDefectos,
            ChecklistCumpleRequisitos = SyncChecklistCumple(dto),
            FechaObservacion = DateTime.UtcNow,
            Firmado = false
        };

        await _actaRepository.AddAsync(acta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Observación registrada. Complete el checklist y firme cuando corresponda.");
    }

    public async Task<ServiceResult> UpdateAsync(
        int id,
        GuardarActaVerificacionDto dto,
        int actorUserId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default)
    {
        var acta = await _actaRepository.GetByIdAsync(id, cancellationToken);
        if (acta is null)
            return ServiceResult.Fail("El acta no existe.");

        if (acta.Firmado)
            return ServiceResult.Fail("El acta ya está firmada y no se puede editar.");

        if (IsAdmin(actorRole))
            return ServiceResult.Fail("El administrador puede consultar actas, pero no editarlas ni firmarlas en nombre del instructor.");

        if (!IsInstructorViewer(actorRole, actorUserId))
            return ServiceResult.Fail("Solo un instructor asignado puede editar el acta.");

        if (acta.InstructorId != actorUserId || !BelongsToInstructor(acta.Ficha, actorUserId, actorName))
            return ServiceResult.Fail("Solo puede actuar sobre sus propias fichas.");

        var validation = ValidateChecklistAndObservacion(dto);
        if (validation is not null) return validation;

        // Orden y ficha quedan fijas tras crear; solo se actualiza observación/checklist
        acta.Observacion = dto.Observacion.Trim();
        acta.CumpleEspecificaciones = dto.CumpleEspecificaciones;
        acta.CumpleAcabados = dto.CumpleAcabados;
        acta.CumpleSinDefectos = dto.CumpleSinDefectos;
        acta.ChecklistCumpleRequisitos = SyncChecklistCumple(dto);
        acta.FechaObservacion = DateTime.UtcNow;

        _actaRepository.Update(acta);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Observación actualizada.");
    }

    public async Task<ServiceResult> FirmarAsync(
        int id,
        int actorUserId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default)
    {
        var acta = await _actaRepository.GetByIdAsync(id, cancellationToken);
        if (acta is null)
            return ServiceResult.Fail("El acta no existe.");

        if (acta.Firmado)
            return ServiceResult.Fail("El acta ya está firmada.");

        if (IsAdmin(actorRole))
            return ServiceResult.Fail("El administrador puede consultar actas, pero no firmar en nombre del instructor.");

        if (!IsInstructorViewer(actorRole, actorUserId))
            return ServiceResult.Fail("Solo un instructor asignado puede firmar el acta.");

        if (acta.InstructorId != actorUserId || !BelongsToInstructor(acta.Ficha, actorUserId, actorName))
            return ServiceResult.Fail("Solo puede firmar actas de sus propias fichas.");

        if (!ChecklistListoParaFirmar(acta))
            return ServiceResult.Fail("No se puede firmar: el checklist debe estar completo y marcado como cumplido.");

        if (string.IsNullOrWhiteSpace(actorName))
            return ServiceResult.Fail("No se pudo obtener el nombre del firmante.");

        acta.Firmado = true;
        acta.FechaFirma = DateTime.UtcNow;
        acta.NombreFirmante = actorName.Trim();
        acta.ChecklistCumpleRequisitos = true;

        _actaRepository.Update(acta);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Acta firmada correctamente.");
    }

    private static ServiceResult? ValidateChecklistAndObservacion(GuardarActaVerificacionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Observacion))
            return ServiceResult.Fail("La observación es obligatoria.");
        if (dto.Observacion.Trim().Length > 2000)
            return ServiceResult.Fail("La observación no puede superar 2000 caracteres.");
        return null;
    }

    // Confirmación global solo si todos los ítems están marcados
    private static bool SyncChecklistCumple(GuardarActaVerificacionDto dto) =>
        dto.CumpleEspecificaciones
        && dto.CumpleAcabados
        && dto.CumpleSinDefectos
        && dto.ChecklistCumpleRequisitos;

    private static bool ChecklistListoParaFirmar(ActaVerificacion acta) =>
        acta.CumpleEspecificaciones
        && acta.CumpleAcabados
        && acta.CumpleSinDefectos
        && acta.ChecklistCumpleRequisitos;

    private static bool CanView(
        ActaVerificacion acta,
        int? viewerUserId,
        string? viewerRole,
        string? viewerName)
    {
        if (IsAdmin(viewerRole)) return true;
        if (!IsInstructorViewer(viewerRole, viewerUserId)) return false;
        return BelongsToInstructor(acta.Ficha, viewerUserId!.Value, viewerName)
               || acta.InstructorId == viewerUserId;
    }

    private static ActaVerificacionDto ToDto(
        ActaVerificacion a,
        int? viewerUserId,
        string? viewerRole)
    {
        var isOwnerInstructor = IsInstructorViewer(viewerRole, viewerUserId)
                                && a.InstructorId == viewerUserId
                                && !a.Firmado;

        return new ActaVerificacionDto(
            a.Id,
            a.ProductionOrderId,
            a.ProductionOrder.OrderNumber,
            a.ProductionOrder.ProductName,
            a.FichaId,
            a.Ficha.FichaCode,
            a.InstructorId,
            a.Instructor.Nombre,
            a.Observacion,
            a.CumpleEspecificaciones,
            a.CumpleAcabados,
            a.CumpleSinDefectos,
            a.ChecklistCumpleRequisitos,
            a.FechaObservacion,
            a.FechaFirma,
            a.Firmado,
            a.NombreFirmante,
            PuedeFirmarse: isOwnerInstructor && ChecklistListoParaFirmar(a),
            PuedeEditar: isOwnerInstructor);
    }

    private static bool IsAdmin(string? role) =>
        string.Equals(role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);

    private static bool IsInstructorViewer(string? viewerRole, int? viewerUserId) =>
        viewerUserId is > 0
        && string.Equals(viewerRole, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);

    // Mismo criterio que FichaService / SolicitudMaterialService
    private static bool BelongsToInstructor(Ficha ficha, int instructorUserId, string? instructorName)
    {
        if (ficha.Instructors.Any(i => i.UserId == instructorUserId))
            return true;

        if (ficha.InstructorUserId == instructorUserId)
            return true;

        return ficha.InstructorUserId is null
               && ficha.Instructors.Count == 0
               && !string.IsNullOrWhiteSpace(instructorName)
               && string.Equals(ficha.InstructorName, instructorName, StringComparison.OrdinalIgnoreCase);
    }
}
