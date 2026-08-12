using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// KPIs y datos del dashboard
public interface IStatisticsService
{
    // viewer*: Instructor → solo órdenes/calidad de su alcance (mismo criterio que GetOrdersAsync / Reportes)
    Task<DashboardKpiDto> GetDashboardAsync(
        int? viewerUserId = null,
        string? viewerRole = null,
        string? viewerName = null,
        CancellationToken cancellationToken = default);
}
