using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IReportService
{
    Task<ReportFileDto> ExportInventoryAsync(string format, CancellationToken cancellationToken = default);
    Task<ReportFileDto> ExportOrdersAsync(string format, CancellationToken cancellationToken = default);
    Task<ReportFileDto> ExportQualityAsync(string format, CancellationToken cancellationToken = default);
    Task<ReportFileDto> ExportDashboardAsync(string format, CancellationToken cancellationToken = default);
    Task<ReportFileDto> ExportMonthlyAsync(int year, int month, string format, CancellationToken cancellationToken = default);
}
