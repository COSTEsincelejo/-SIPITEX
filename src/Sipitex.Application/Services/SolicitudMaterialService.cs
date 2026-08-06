using Sipitex.Application.DTOs;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Application.Interfaces.Services;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Services;

// Crear y consultar SolicitudMaterial (scope Admin / Instructor)
public class SolicitudMaterialService : ISolicitudMaterialService
{
    private readonly ISolicitudMaterialRepository _solicitudRepository;
    private readonly IFichaRepository _fichaRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly ICodigoGeneradorService _codigoGenerador;
    private readonly IUnitOfWork _unitOfWork;

    public SolicitudMaterialService(
        ISolicitudMaterialRepository solicitudRepository,
        IFichaRepository fichaRepository,
        IMaterialRepository materialRepository,
        ICodigoGeneradorService codigoGenerador,
        IUnitOfWork unitOfWork)
    {
        _solicitudRepository = solicitudRepository;
        _fichaRepository = fichaRepository;
        _materialRepository = materialRepository;
        _codigoGenerador = codigoGenerador;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> CreateAsync(
        CreateSolicitudMaterialDto dto,
        int solicitanteId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default)
    {
        if (solicitanteId <= 0)
            return ServiceResult.Fail("Usuario no válido.");

        var lineas = (dto.Detalles ?? [])
            .Where(d => d.MaterialId > 0 && d.CantidadSolicitada > 0)
            .GroupBy(d => d.MaterialId)
            .Select(g => new CreateDetalleSolicitudDto(g.Key, g.Sum(x => x.CantidadSolicitada)))
            .ToList();

        if (lineas.Count == 0)
            return ServiceResult.Fail("Agregue al menos un material con cantidad mayor a cero.");

        var ficha = await _fichaRepository.GetByIdAsync(dto.FichaId, cancellationToken);
        if (ficha is null)
            return ServiceResult.Fail("Ficha no encontrada.");

        if (!CanRequestOnFicha(ficha, solicitanteId, actorRole, actorName))
            return ServiceResult.Fail("No tiene permiso para solicitar materiales en esta ficha.");

        foreach (var linea in lineas)
        {
            var material = await _materialRepository.GetByIdAsync(linea.MaterialId, cancellationToken);
            if (material is null)
                return ServiceResult.Fail("Uno de los materiales seleccionados no existe.");
        }

        var codigo = await _codigoGenerador.GenerarCodigoSolicitudMaterialAsync(cancellationToken);
        var observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
            ? null
            : dto.Observaciones.Trim();

        var solicitud = new SolicitudMaterial
        {
            Codigo = codigo,
            FichaId = ficha.Id,
            SolicitanteId = solicitanteId,
            Estado = SolicitudMaterialEstado.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            Observaciones = observaciones,
            Detalles = lineas.Select(l => new DetalleSolicitudMaterial
            {
                MaterialId = l.MaterialId,
                CantidadSolicitada = l.CantidadSolicitada,
                CantidadAprobada = null,
                EstadoItem = DetalleSolicitudEstado.Pendiente
            }).ToList()
        };

        await _solicitudRepository.AddAsync(solicitud, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok($"Solicitud {codigo} creada.");
    }

    public async Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListAsync(
        int? viewerUserId,
        string? viewerRole,
        CancellationToken cancellationToken = default)
    {
        var all = await _solicitudRepository.GetAllWithFichaAsync(cancellationToken);

        IEnumerable<SolicitudMaterial> scoped = all;
        if (IsInstructor(viewerRole, viewerUserId))
            scoped = all.Where(s => s.SolicitanteId == viewerUserId);
        else if (!IsAdmin(viewerRole))
            scoped = [];

        return scoped
            .Select(s => new SolicitudMaterialListItemDto(
                s.Id,
                s.Codigo,
                s.Ficha?.FichaCode ?? "—",
                s.Estado,
                s.FechaSolicitud,
                s.Solicitante?.Nombre ?? "—"))
            .ToList();
    }

    public async Task<SolicitudMaterialDetailDto?> GetDetailAsync(
        int id,
        int? viewerUserId,
        string? viewerRole,
        CancellationToken cancellationToken = default)
    {
        var solicitud = await _solicitudRepository.GetByIdWithDetallesAsync(id, cancellationToken);
        if (solicitud is null)
            return null;

        if (IsInstructor(viewerRole, viewerUserId) && solicitud.SolicitanteId != viewerUserId)
            return null;

        if (!IsAdmin(viewerRole) && !IsInstructor(viewerRole, viewerUserId))
            return null;

        return new SolicitudMaterialDetailDto(
            solicitud.Id,
            solicitud.Codigo,
            solicitud.Ficha?.FichaCode ?? "—",
            solicitud.Solicitante?.Nombre ?? "—",
            solicitud.Estado,
            solicitud.FechaSolicitud,
            solicitud.FechaResolucion,
            solicitud.Observaciones,
            solicitud.Detalles
                .OrderBy(d => d.Id)
                .Select(d => new DetalleSolicitudMaterialDto(
                    d.Id,
                    d.Material?.Name ?? "—",
                    d.Material is null ? "—" : UnitHelper.ToDisplay(d.Material.Unit),
                    d.CantidadSolicitada,
                    d.CantidadAprobada,
                    d.EstadoItem))
                .ToList());
    }

    // Admin: cualquier ficha. Instructor: solo si está asignado (misma regla M2M/legacy que Fichas)
    private static bool CanRequestOnFicha(
        Ficha ficha,
        int actorUserId,
        string? actorRole,
        string? actorName)
    {
        if (IsAdmin(actorRole))
            return true;

        return IsInstructor(actorRole, actorUserId)
               && BelongsToInstructor(ficha, actorUserId, actorName);
    }

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

    private static bool IsAdmin(string? role) =>
        string.Equals(role, UserRoles.Administrador, StringComparison.OrdinalIgnoreCase);

    private static bool IsInstructor(string? role, int? userId) =>
        userId is > 0
        && string.Equals(role, UserRoles.Instructor, StringComparison.OrdinalIgnoreCase);
}
