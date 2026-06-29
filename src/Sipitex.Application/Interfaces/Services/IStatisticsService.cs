using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface IStatisticsService
{
    Task<DashboardKpiDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
