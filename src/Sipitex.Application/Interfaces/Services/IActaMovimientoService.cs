using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IActaMovimientoService
{
    Task<IReadOnlyList<ActaMovimientoDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ActaMovimientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<ActaMovimientoDto>> CreateAsync(CreateActaDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<ReportFileDto>> ExportPdfAsync(int id, CancellationToken cancellationToken = default);
}

public interface IActaPdfService
{
    ReportFileDto Render(ActaMovimientoDto acta);
}
