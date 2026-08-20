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

    // Listado para Bodeguero de una bodega; soloPendientes=true filtra Estado=Pendiente
    Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListForBodegaAsync(
        int bodegaId,
        bool soloPendientes = true,
        CancellationToken cancellationToken = default);

    // Detalle con stock actual para resolución; null si no existe o es de otra bodega
    Task<SolicitudMaterialResolucionDto?> GetResolucionDetailAsync(
        int id,
        int bodegaId,
        CancellationToken cancellationToken = default);
}
