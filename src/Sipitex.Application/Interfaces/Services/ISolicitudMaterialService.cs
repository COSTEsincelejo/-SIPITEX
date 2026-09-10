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

    // Listado para EncargadoDeBodega de las plantasInventario asignadas.
    // viewerPlantaInventarioIds null o vacío → lista vacía (encargadoDeBodega sin asignaciones: el controlador bloquea).
    Task<IReadOnlyList<SolicitudMaterialListItemDto>> GetListForPlantaInventarioAsync(
        IReadOnlyList<int>? viewerPlantaInventarioIds,
        bool soloPendientes = true,
        CancellationToken cancellationToken = default);

    // Detalle con stock actual para resolución. null si no existe, viewer sin plantasInventario, o es de otra plantaInventario.
    Task<SolicitudMaterialResolucionDto?> GetResolucionDetailAsync(
        int id,
        IReadOnlyList<int>? viewerPlantaInventarioIds,
        CancellationToken cancellationToken = default);
}
