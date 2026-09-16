using Microsoft.EntityFrameworkCore;
using Sipitex.Application.Interfaces.Repositories;
using Sipitex.Domain.Entities;
using Sipitex.Infrastructure.Persistence;

namespace Sipitex.Infrastructure.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly SipitexDbContext _db;

    public AppSettingRepository(SipitexDbContext db) => _db = db;

    public Task<AppSetting?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

    public async Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var existing = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (existing is null)
        {
            _db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                UpdatedUtc = DateTime.UtcNow
            });
            return;
        }

        existing.Value = value;
        existing.UpdatedUtc = DateTime.UtcNow;
    }
}
