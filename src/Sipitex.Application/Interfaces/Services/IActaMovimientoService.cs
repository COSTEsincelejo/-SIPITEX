using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IActaMovimientoService
{
    Task<IReadOnlyList<ActaMovimientoDto>> GetAllAsync(
        ActaViewerFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<ActaMovimientoDto?> GetByIdAsync(
        int id,
        ActaViewerFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ActaMovimientoDto>> CreateAsync(CreateActaDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<ReportFileDto>> ExportPdfAsync(int id, CancellationToken cancellationToken = default);
}

public record ActaViewerFilter(
    string Role,
    int UserId,
    IReadOnlyList<int> PlantaInventarioIds,
    IReadOnlyCollection<int> AllowedOrderIds);

public interface IActaPdfService
{
    ReportFileDto Render(ActaMovimientoDto acta);
}
