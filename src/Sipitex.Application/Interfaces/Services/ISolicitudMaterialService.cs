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

    // Listado para Bodeguero (todas las fichas); soloPendientes=true filtra Estado=Pendiente
    Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListForBodegaAsync(
        bool soloPendientes = true,
        CancellationToken cancellationToken = default);

    // Detalle con stock actual para resolución en bodega
    Task<SolicitudMaterialResolucionDto?> GetResolucionDetailAsync(
        int id,
        CancellationToken cancellationToken = default);
}
