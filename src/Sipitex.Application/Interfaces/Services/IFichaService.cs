using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Fichas de producción y registro de sesiones diarias
public interface IFichaService
{
    // Lista fichas (filtrada si el viewer es instructor)
    Task<IReadOnlyList<FichaDto>> GetFichasAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    // Instructores activos registrados (para selects)
    Task<IReadOnlyList<InstructorOptionDto>> GetActiveInstructorsAsync(
        CancellationToken cancellationToken = default);

    // Sesiones recientes de producción
    Task<IReadOnlyList<ProductionSessionDto>> GetRecentSessionsAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    // Registrar sesión con validación de permisos de instructor
    Task<ServiceResult> RegisterSessionAsync(
        RegisterProductionDto dto,
        int? registeredByUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    // Registro rápido usando la orden ya asignada a la ficha
    Task<ServiceResult> QuickRegisterAsync(
        int fichaId,
        int units,
        string? observations = null,
        int? registeredByUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);

    // Crear ficha nueva con uno o más instructores registrados
    Task<ServiceResult> CreateFichaAsync(
        CreateFichaDto dto,
        CancellationToken cancellationToken = default);

    // Asignar un instructor registrado a una ficha existente
    Task<ServiceResult> AssignInstructorAsync(
        int fichaId,
        int instructorUserId,
        int? actorUserId = null,
        string? actorRole = null,
        string? actorName = null,
        string? proceso = null,
        CancellationToken cancellationToken = default);

    // Quitar un instructor de una ficha
    Task<ServiceResult> RemoveInstructorAsync(
        int fichaId,
        int instructorUserId,
        int? actorUserId = null,
        string? actorRole = null,
        string? actorName = null,
        CancellationToken cancellationToken = default);

    // Actualizar el proceso de un instructor en una ficha (Admin cualquiera; Instructor solo el suyo)
    Task<ServiceResult> UpdateInstructorProcesoAsync(
        int fichaId,
        int instructorUserId,
        string? proceso,
        int? actorUserId = null,
        string? actorRole = null,
        string? actorName = null,
        CancellationToken cancellationToken = default);
}
