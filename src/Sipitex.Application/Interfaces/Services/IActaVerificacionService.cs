using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Observación y firma de actas de verificación sobre órdenes de producción
public interface IActaVerificacionService
{
    Task<IReadOnlyList<ActaVerificacionDto>> GetActasAsync(
        int? viewerUserId,
        string? viewerRole,
        string? viewerName,
        CancellationToken cancellationToken = default);

    Task<ActaVerificacionDto?> GetByIdAsync(
        int id,
        int? viewerUserId,
        string? viewerRole,
        string? viewerName,
        CancellationToken cancellationToken = default);

    // Crea borrador con observación y checklist (solo instructor de la ficha)
    Task<ServiceResult> CreateAsync(
        GuardarActaVerificacionDto dto,
        int actorUserId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default);

    // Actualiza observación/checklist si aún no está firmada
    Task<ServiceResult> UpdateAsync(
        int id,
        GuardarActaVerificacionDto dto,
        int actorUserId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default);

    // Firma el acta (solo instructor dueño; admin puede ver pero no firmar)
    Task<ServiceResult> FirmarAsync(
        int id,
        int actorUserId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default);
}
