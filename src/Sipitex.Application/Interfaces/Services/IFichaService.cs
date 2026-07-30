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

    // Crear ficha nueva
    Task<ServiceResult> CreateFichaAsync(
        CreateFichaDto dto,
        int? instructorUserId = null,
        CancellationToken cancellationToken = default);
}
