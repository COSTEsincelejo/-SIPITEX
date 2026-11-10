using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

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

    Task<ServiceResult<ActaMovimientoDto>> FirmarRecibeAsync(
        int actaId,
        int actorUserId,
        string actorRole,
        bool conforme,
        byte[]? firmaPng,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ActaMovimientoDto>> AnularAsync(
        int actaId,
        int actorUserId,
        string actorRole,
        string motivo,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<ReportFileDto>> ExportPdfAsync(
        int id,
        CancellationToken cancellationToken = default,
        string? urlVerificacion = null);

    Task<VerificacionActaResultado> VerificarPublicoAsync(
        string? numero,
        string? codigo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> GetActasPorMovimientoStockAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, string>> GetActasPorConsumoAsync(CancellationToken cancellationToken = default);
}

public record ActaViewerFilter(
    string Role,
    int UserId,
    IReadOnlyList<int> PlantaInventarioIds,
    IReadOnlyCollection<int> AllowedOrderIds);

public sealed record VerificacionActaResultado(
    bool Autentica,
    string? Numero,
    ActaTipo? Tipo,
    DateTime? FechaUtc,
    ActaEstado? Estado,
    int CantidadItems);

public interface IActaPdfService
{
    ReportFileDto Render(ActaMovimientoDto acta, string? urlVerificacion = null);
    string CodigoVerificacion(ActaMovimientoDto acta);
}
