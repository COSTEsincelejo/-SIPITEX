using Microsoft.EntityFrameworkCore;
using Sipitex.Application.DTOs;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Domain.Enums;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly SipitexDbContext _context;

    public AlertRepository(SipitexDbContext context) => _context = context;

    public async Task<IReadOnlyList<AlertPreference>> GetPreferencesByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        await _context.AlertPreferences.Where(p => p.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AlertPreference>> GetEnabledPreferencesAsync(AlertType type, CancellationToken cancellationToken = default) =>
        await _context.AlertPreferences
            .Include(p => p.User)
            .Where(p => p.AlertType == type && p.Enabled && p.User.IsActive)
            .ToListAsync(cancellationToken);

    public async Task UpsertPreferencesAsync(int userId, IReadOnlyDictionary<AlertType, bool> preferences, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AlertPreferences.Where(p => p.UserId == userId).ToListAsync(cancellationToken);
        foreach (var (type, enabled) in preferences)
        {
            var pref = existing.FirstOrDefault(p => p.AlertType == type);
            if (pref is null)
            {
                _context.AlertPreferences.Add(new AlertPreference
                {
                    UserId = userId,
                    AlertType = type,
                    Enabled = enabled
                });
            }
            else
            {
                pref.Enabled = enabled;
            }
        }
    }

    public async Task AddDeliveryAsync(AlertDelivery delivery, CancellationToken cancellationToken = default) =>
        await _context.AlertDeliveries.AddAsync(delivery, cancellationToken);

    public async Task<IReadOnlyList<AlertDelivery>> GetRecentDeliveriesAsync(int take, CancellationToken cancellationToken = default) =>
        await _context.AlertDeliveries
            .Include(d => d.User)
            .OrderByDescending(d => d.SentAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task EnsureDefaultPreferencesAsync(User user, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AlertPreferences.Where(p => p.UserId == user.Id).Select(p => p.AlertType).ToListAsync(cancellationToken);
        foreach (var item in AlertCatalog.All)
        {
            if (existing.Contains(item.Type)) continue;
            var enabledByRole = item.Roles.Contains(user.Rol);
            _context.AlertPreferences.Add(new AlertPreference
            {
                UserId = user.Id,
                AlertType = item.Type,
                Enabled = enabledByRole
            });
        }
    }
}
