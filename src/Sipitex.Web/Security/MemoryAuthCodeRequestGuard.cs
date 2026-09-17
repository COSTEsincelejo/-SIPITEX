using Microsoft.Extensions.Caching.Memory;
using Sipitex.Application.Helpers;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Web.Security;

public sealed class MemoryAuthCodeRequestGuard : IAuthCodeRequestGuard
{
    public const int MaxRequestsPerWindow = 8;

    private readonly IMemoryCache _cache;

    public MemoryAuthCodeRequestGuard(IMemoryCache cache) => _cache = cache;

    public bool IsLimited(string? ipAddress, string purpose)
    {
        if (!_cache.TryGetValue(Key(ipAddress, purpose), out int count))
            return false;
        return count >= MaxRequestsPerWindow;
    }

    public void Record(string? ipAddress, string purpose)
    {
        var key = Key(ipAddress, purpose);
        var count = _cache.Get<int>(key) + 1;
        _cache.Set(key, count, AuthCodeHelper.RateLimitWindow);
    }

    private static string Key(string? ipAddress, string purpose) =>
        $"auth-code-ip:{purpose}|{(string.IsNullOrWhiteSpace(ipAddress) ? "-" : ipAddress.Trim())}";
}
