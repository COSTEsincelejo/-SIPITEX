using Microsoft.Extensions.Caching.Memory;
using Sipitex.Application.Interfaces.Services;

namespace Sipitex.Web.Security;

// Bloqueo en memoria: 5 fallos en 15 minutos (email + IP). Se reinicia al arrancar el proceso.
public sealed class MemoryLoginAttemptGuard : ILoginAttemptGuard
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryLoginAttemptGuard> _logger;

    public MemoryLoginAttemptGuard(IMemoryCache cache, ILogger<MemoryLoginAttemptGuard> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public bool IsLockedOut(string email, string? ipAddress)
    {
        if (!_cache.TryGetValue(Key(email, ipAddress), out LoginAttemptState? state) || state is null)
            return false;
        return state.IsLocked(DateTimeOffset.UtcNow);
    }

    public void RecordFailure(string email, string? ipAddress)
    {
        var key = Key(email, ipAddress);
        var now = DateTimeOffset.UtcNow;
        var state = _cache.Get<LoginAttemptState>(key);
        if (state is null || now - state.WindowStart > LockoutDuration)
        {
            state = new LoginAttemptState { WindowStart = now };
        }

        state.FailedCount++;
        if (state.FailedCount >= MaxFailedAttempts)
        {
            state.LockedUntil = now.Add(LockoutDuration);
            _logger.LogWarning(
                "Login bloqueado temporalmente para {Email} desde {Ip} tras {Count} intentos fallidos",
                Normalize(email),
                string.IsNullOrWhiteSpace(ipAddress) ? "-" : ipAddress,
                state.FailedCount);
        }
        else
        {
            _logger.LogInformation(
                "Login fallido ({Count}/{Max}) para {Email} desde {Ip}",
                state.FailedCount,
                MaxFailedAttempts,
                Normalize(email),
                string.IsNullOrWhiteSpace(ipAddress) ? "-" : ipAddress);
        }

        var expires = state.LockedUntil ?? state.WindowStart.Add(LockoutDuration);
        _cache.Set(key, state, expires);
    }

    public void Reset(string email, string? ipAddress) => _cache.Remove(Key(email, ipAddress));

    private static string Key(string email, string? ipAddress) =>
        $"login-attempts:{Normalize(email)}|{(string.IsNullOrWhiteSpace(ipAddress) ? "-" : ipAddress.Trim())}";

    private static string Normalize(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private sealed class LoginAttemptState
    {
        public int FailedCount { get; set; }
        public DateTimeOffset WindowStart { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }

        public bool IsLocked(DateTimeOffset now) =>
            LockedUntil is DateTimeOffset until && now < until;
    }
}
