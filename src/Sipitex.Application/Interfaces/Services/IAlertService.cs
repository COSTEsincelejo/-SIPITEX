using Sipitex.Application.DTOs;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Interfaces.Services;

public interface IAlertService
{
    Task<IReadOnlyList<AlertPreferenceDto>> GetPreferencesForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task SavePreferencesAsync(int userId, IReadOnlyDictionary<AlertType, bool> preferences, CancellationToken cancellationToken = default);
    Task<AlertEvaluationResultDto> EvaluateAndSendAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertDeliveryDto>> GetRecentDeliveriesAsync(int take = 30, CancellationToken cancellationToken = default);
}
