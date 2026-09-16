using Sipitex.Domain.Entities;

namespace Sipitex.Application.Interfaces.Repositories;

public interface IAppSettingRepository
{
    Task<AppSetting?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task UpsertAsync(string key, string value, CancellationToken cancellationToken = default);
}
