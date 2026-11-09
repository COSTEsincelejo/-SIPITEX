using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Exportación de reportes a archivo (inventario, órdenes, calidad, dashboard)
public interface IReportService
{
    // format = pdf, xlsx, etc. | filter opcional (sin filtro = reporte completo)
    Task<ReportFileDto> ExportInventoryAsync(
        string format,
        ReportFilterDto? filter = null,
        CancellationToken cancellationToken = default);

    Task<ReportFileDto> ExportOrdersAsync(
        string format,
        ReportFilterDto? filter = null,
        CancellationToken cancellationToken = default);

    Task<ReportFileDto> ExportQualityAsync(
        string format,
        ReportFilterDto? filter = null,
        CancellationToken cancellationToken = default);

    Task<ReportFileDto> ExportDashboardAsync(
        string format,
        ReportFilterDto? filter = null,
        CancellationToken cancellationToken = default);
}
