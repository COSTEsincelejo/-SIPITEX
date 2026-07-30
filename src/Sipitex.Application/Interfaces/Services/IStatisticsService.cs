using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

// KPIs y datos del dashboard
public interface IStatisticsService
{
    Task<DashboardKpiDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
