using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface ITrazabilidadService
{
    Task<IReadOnlyList<PrendaTrazableListDto>> SearchAsync(
        TrazabilidadViewerFilter filter,
        string? query,
        int? productionOrderId,
        CancellationToken cancellationToken = default);

    Task<PrendaTrazableDetailDto?> GetByCodigoAsync(
        TrazabilidadViewerFilter filter,
        string codigo,
        CancellationToken cancellationToken = default);

    Task<PrendaTrazableDetailDto?> GetByIdAsync(
        TrazabilidadViewerFilter filter,
        int id,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<PrendaTrazableListDto>>> GenerarAsync(
        TrazabilidadViewerFilter filter,
        int productionOrderId,
        int cantidad,
        int actorUserId,
        CancellationToken cancellationToken = default);
}

public record TrazabilidadViewerFilter(
    string Role,
    int UserId,
    IReadOnlyCollection<int> AllowedOrderIds);
