namespace StoneBridge.Application.Common.Interfaces;

/// <summary>
/// Abstracts Redis caching operations.
/// Never throws on cache failure — logs the error and returns null / executes factory.
/// A cache miss or Redis outage must never break a request.
/// </summary>
public interface ICacheService
{
    /// <summary>Get a cached value. Returns null if key does not exist or has expired.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>Store a value with an optional absolute expiry.</summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class;

    /// <summary>Remove a key. No-op if key does not exist.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Get-or-set: return cached value, or execute factory, cache the result, return it.
    /// This is the primary caching pattern — prefer this over separate Get + Set calls.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class;
}