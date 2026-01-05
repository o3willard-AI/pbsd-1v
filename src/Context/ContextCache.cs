using System.Collections.Concurrent;

namespace PairAdmin.Context;

/// <summary>
/// Cache hit information
/// </summary>
public class CacheHitInfo
{
    /// <summary>
    /// Whether cache hit occurred
    /// </summary>
    public bool IsHit { get; set; }

    /// <summary>
    /// Timestamp of cache entry
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Age of cached entry
    /// </summary>
    public TimeSpan Age => DateTime.Now - Timestamp;
}

/// <summary>
/// Cache entry with TTL
/// </summary>
internal class CacheEntry
{
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }

    public CacheEntry(string content)
    {
        Content = content;
        Timestamp = DateTime.Now;
    }

    public bool IsExpired(TimeSpan ttl)
    {
        return DateTime.Now - Timestamp > ttl;
    }
}

/// <summary>
/// Interface for context cache service
/// </summary>
public interface IContextCache
{
    /// <summary>
    /// Stores context in cache
    /// </summary>
    /// <param name="context">Context to store</param>
    void Store(string context);

    /// <summary>
    /// Stores context in cache with specific key
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="context">Context to store</param>
    void Store(string cacheKey, string context);

    /// <summary>
    /// Retrieves context from cache
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <returns>Cached context or null if not found/expired</returns>
    string? Retrieve(string cacheKey);

    /// <summary>
    /// Invalidates cache entry
    /// </summary>
    /// <param name="cacheKey">Cache key (null to invalidate all)</param>
    void Invalidate(string? cacheKey);

    /// <summary>
    /// Clears all cache entries
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Checks if cache entry is available
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <returns>True if cache entry exists and is not expired</returns>
    bool IsAvailable(string cacheKey);

    /// <summary>
    /// Gets cache hit information
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <returns>Cache hit info or null if not found</returns>
    CacheHitInfo? GetCacheHitInfo(string cacheKey);
}

/// <summary>
/// In-memory context cache with TTL support
/// Thread-safe implementation using ConcurrentDictionary
/// </summary>
public class ContextCache : IContextCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly TimeSpan _ttl;
    private int _hitCount;
    private int _missCount;
    private readonly object _statsLock = new object();

    /// <summary>
    /// Cache hit count
    /// </summary>
    public int HitCount
    {
        get
        {
            lock (_statsLock)
            {
                return _hitCount;
            }
        }
    }

    /// <summary>
    /// Cache miss count
    /// </summary>
    public int MissCount
    {
        get
        {
            lock (_statsLock)
            {
                return _missCount;
            }
        }
    }

    /// <summary>
    /// Gets cache hit rate
    /// </summary>
    public double HitRate
    {
        get
        {
            lock (_statsLock)
            {
                var total = _hitCount + _missCount;
                return total > 0 ? (double)_hitCount / total : 0.0;
            }
        }
    }

    /// <summary>
    /// Gets number of entries in cache
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Initializes a new instance with default TTL (5 minutes)
    /// </summary>
    public ContextCache() : this(TimeSpan.FromMinutes(5))
    {
    }

    /// <summary>
    /// Initializes a new instance with custom TTL
    /// </summary>
    /// <param name="ttl">Time-to-live for cache entries</param>
    public ContextCache(TimeSpan ttl)
    {
        _ttl = ttl;
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _hitCount = 0;
        _missCount = 0;
    }

    /// <summary>
    /// Stores context in cache
    /// </summary>
    /// <param name="context">Context to store</param>
    public void Store(string context)
    {
        Store("default", context);
    }

    /// <summary>
    /// Stores context in cache with specific key
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="context">Context to store</param>
    public void Store(string cacheKey, string context)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            throw new ArgumentNullException(nameof(cacheKey));
        }

        var entry = new CacheEntry(context);
        _cache.AddOrUpdate(cacheKey, entry, (key, old) => entry);
    }

    /// <summary>
    /// Retrieves context from cache
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <returns>Cached context or null if not found/expired</returns>
    public string? Retrieve(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            throw new ArgumentNullException(nameof(cacheKey));
        }

        if (_cache.TryGetValue(cacheKey, out var entry))
        {
            if (!entry.IsExpired(_ttl))
            {
                lock (_statsLock)
                {
                    _hitCount++;
                }
                return entry.Content;
            }
            else
            {
                _cache.TryRemove(cacheKey, out _);
            }
        }

        lock (_statsLock)
        {
            _missCount++;
        }
        return null;
    }

    /// <summary>
    /// Invalidates cache entry
    /// </summary>
    /// <param name="cacheKey">Cache key (null to invalidate all)</param>
    public void Invalidate(string? cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            ClearAll();
        }
        else
        {
            _cache.TryRemove(cacheKey, out _);
        }
    }

    /// <summary>
    /// Clears all cache entries
    /// </summary>
    public void ClearAll()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Checks if cache entry is available
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <returns>True if cache entry exists and is not expired</returns>
    public bool IsAvailable(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            return false;
        }

        if (_cache.TryGetValue(cacheKey, out var entry))
        {
            if (!entry.IsExpired(_ttl))
            {
                return true;
            }
            else
            {
                _cache.TryRemove(cacheKey, out _);
            }
        }

        return false;
    }

    /// <summary>
    /// Gets cache hit information
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <returns>Cache hit info or null if not found</returns>
    public CacheHitInfo? GetCacheHitInfo(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            return null;
        }

        if (_cache.TryGetValue(cacheKey, out var entry))
        {
            return new CacheHitInfo
            {
                IsHit = true,
                Timestamp = entry.Timestamp
            };
        }

        return null;
    }

    /// <summary>
    /// Removes expired entries from cache
    /// </summary>
    public void CleanupExpired()
    {
        var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired(_ttl))
                                 .Select(kvp => kvp.Key)
                                 .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }
}
