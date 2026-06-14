using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Infrastructure.Services;

/// <summary>
/// Redis-backed cache using IDistributedCache (StackExchange.Redis under the hood).
/// IMPORTANT: Every method is wrapped in try/catch.
/// A Redis outage must never break a request — the application degrades gracefully
/// by executing the factory function and returning uncached data.
/// Uses System.Text.Json for serialisation.
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly IDistributedCache         _cache;
    private readonly ILogger<CacheService>     _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        WriteIndented               = false,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public CacheService(
        IDistributedCache     cache,
        ILogger<CacheService> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        where T : class
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null or { Length: 0 })
            {
                return null;
            }
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key '{Key}' — returning null", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string            key,
        T                 value,
        TimeSpan?         expiry = null,
        CancellationToken ct     = default)
        where T : class
    {
        try
        {
            var bytes   = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }

            await _cache.SetAsync(key, bytes, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key '{Key}' — value not cached", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key '{Key}'", key);
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string                           key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan?                        expiry = null,
        CancellationToken                ct     = default)
        where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(ct);
        await SetAsync(key, value, expiry, ct);
        return value;
    }
}