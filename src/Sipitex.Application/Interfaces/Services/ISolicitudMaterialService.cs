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

    // Listado para Bodeguero de las bodegas asignadas.
    // viewerBodegaIds null o vacío → lista vacía (bodeguero sin asignaciones: el controlador bloquea).
    Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListForBodegaAsync(
        IReadOnlyList<int>? viewerBodegaIds,
        bool soloPendientes = true,
        CancellationToken cancellationToken = default);

    // Detalle con stock actual para resolución. null si no existe, viewer sin bodegas, o es de otra bodega.
    Task<SolicitudMaterialResolucionDto?> GetResolucionDetailAsync(
        int id,
        IReadOnlyList<int>? viewerBodegaIds,
        CancellationToken cancellationToken = default);
}
