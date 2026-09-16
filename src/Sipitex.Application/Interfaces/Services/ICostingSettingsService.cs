using Sipitex.Application.DTOs;

namespace Sipitex.Application.Interfaces.Services;

public interface ICostingSettingsService
{
    Task<decimal> GetLaborHourRateAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<decimal>> UpdateLaborHourRateAsync(
        decimal rate,
        int actorUserId,
        CancellationToken cancellationToken = default);
}
