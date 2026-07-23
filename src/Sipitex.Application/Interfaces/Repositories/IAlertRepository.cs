using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IAlertRepository
{
    Task<IReadOnlyList<AlertPreference>> GetPreferencesByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertPreference>> GetEnabledPreferencesAsync(AlertType type, CancellationToken cancellationToken = default);
    Task UpsertPreferencesAsync(int userId, IReadOnlyDictionary<AlertType, bool> preferences, CancellationToken cancellationToken = default);
    Task AddDeliveryAsync(AlertDelivery delivery, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertDelivery>> GetRecentDeliveriesAsync(int take, CancellationToken cancellationToken = default);
    Task EnsureDefaultPreferencesAsync(User user, CancellationToken cancellationToken = default);
}
