using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Crear y consultar SolicitudMaterial (flujo Ficha; paralelo a MaterialRequest)
public interface ISolicitudMaterialService
{
    Task<ServiceResult> CreateAsync(
        CreateSolicitudMaterialDto dto,
        int solicitanteId,
        string? actorRole,
        string? actorName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListAsync(
        int? viewerUserId,
        string? viewerRole,
        CancellationToken cancellationToken = default);

    Task<SolicitudMaterialDetailDto?> GetDetailAsync(
        int id,
        int? viewerUserId,
        string? viewerRole,
        CancellationToken cancellationToken = default);

    // Listado para Bodeguero de UNA bodega. viewerBodegaId null o <= 0 → lista vacía
    // (bodeguero sin bodega asignada: el controlador bloquea con mensaje; no se listan todas).
    Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListForBodegaAsync(
        int? viewerBodegaId,
        bool soloPendientes = true,
        CancellationToken cancellationToken = default);

    // Detalle con stock actual para resolución. null si no existe, viewer sin bodega, o es de otra bodega.
    Task<SolicitudMaterialResolucionDto?> GetResolucionDetailAsync(
        int id,
        int? viewerBodegaId,
        CancellationToken cancellationToken = default);
}
