using System.Collections.Concurrent;
using Core.Auth.Configuration;
using Microsoft.Extensions.Options;

namespace Core.Auth.Services;

public class RateLimitService : IRateLimitService, IDisposable
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _attempts = new();
    private readonly CoreAuthOptions _options;
    private readonly Timer _cleanupTimer;

    public RateLimitService(IOptions<CoreAuthOptions> options)
    {
        _options = options.Value;
        _cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromMinutes(30), TimeSpan.FromHours(1));
    }

    public bool IsRateLimited(string key)
    {
        if (!_attempts.TryGetValue(key, out var entry))
            return false;

        if (DateTime.UtcNow - entry.FirstAttempt > TimeSpan.FromMinutes(_options.RateLimitLockoutMinutes))
        {
            _attempts.TryRemove(key, out _);
            return false;
        }

        return entry.Count >= _options.RateLimitMaxAttempts;
    }

    public void RecordAttempt(string key)
    {
        _attempts.AddOrUpdate(key,
            _ => new RateLimitEntry { Count = 1, FirstAttempt = DateTime.UtcNow },
            (_, existing) =>
            {
                if (DateTime.UtcNow - existing.FirstAttempt > TimeSpan.FromMinutes(_options.RateLimitLockoutMinutes))
                    return new RateLimitEntry { Count = 1, FirstAttempt = DateTime.UtcNow };

                existing.Count++;
                return existing;
            });
    }

    public void ResetAttempts(string key)
        => _attempts.TryRemove(key, out _);

    public string BuildLoginKey(string clientIp, string username)
        => $"login:{clientIp}:{username.ToLower().Trim()}";

    private void Cleanup(object? state)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-_options.RateLimitLockoutMinutes * 2);
        foreach (var kvp in _attempts)
        {
            if (kvp.Value.FirstAttempt < cutoff)
                _attempts.TryRemove(kvp.Key, out _);
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        GC.SuppressFinalize(this);
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime FirstAttempt { get; set; }
    }
}
