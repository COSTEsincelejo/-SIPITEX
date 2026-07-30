using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// Exportación de reportes a archivo (inventario, órdenes, calidad, dashboard)
public interface IReportService
{
    Task<ReportFileDto> ExportInventoryAsync(string format, CancellationToken cancellationToken = default);
    Task<ReportFileDto> ExportOrdersAsync(string format, CancellationToken cancellationToken = default);
    Task<ReportFileDto> ExportQualityAsync(string format, CancellationToken cancellationToken = default);
    Task<ReportFileDto> ExportDashboardAsync(string format, CancellationToken cancellationToken = default);
}
