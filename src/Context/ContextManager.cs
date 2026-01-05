using Microsoft.Extensions.Logging;

namespace PairAdmin.Context;

/// <summary>
/// Statistics for context manager
/// </summary>
public class ContextStatistics
{
    /// <summary>
    /// Current line count in context
    /// </summary>
    public int LineCount { get; set; }

    /// <summary>
    /// Current token count in context
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Cache hit count
    /// </summary>
    public int CacheHitCount { get; set; }

    /// <summary>
    /// Cache miss count
    /// </summary>
    public int CacheMissCount { get; set; }

    /// <summary>
    /// Cache hit rate
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Session duration
    /// </summary>
    public TimeSpan SessionDuration { get; set; }

    /// <summary>
    /// Idle time since last activity
    /// </summary>
    public TimeSpan IdleTime { get; set; }
}

/// <summary>
/// Main context manager for providing terminal output context to LLM
/// Integrates SlidingWindowBuffer, ContextCache, and ContextMetadata
/// </summary>
public class ContextManager : IContextProvider
{
    private readonly SlidingWindowBuffer _buffer;
    private readonly IContextTokenCounter _tokenCounter;
    private readonly IContextCache _cache;
    private readonly ContextWindowConfig _config;
    private readonly ContextMetadata _metadata;
    private readonly ILogger<ContextManager> _logger;
    private bool _disposed;

    /// <summary>
    /// Gets current cache hit rate
    /// </summary>
    public double CacheHitRate => _cache.HitRate;

    /// <summary>
    /// Gets current cache hit count
    /// </summary>
    public int CacheHitCount => _cache.HitCount;

    /// <summary>
    /// Gets current cache miss count
    /// </summary>
    public int CacheMissCount => _cache.MissCount;

    /// <summary>
    /// Initializes a new instance of ContextManager
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="tokenCounter">Token counter service</param>
    /// <param name="config">Context window configuration</param>
    public ContextManager(
        ILogger<ContextManager> logger,
        IContextTokenCounter? tokenCounter = null,
        ContextWindowConfig? config = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenCounter = tokenCounter ?? new ContextTokenCounter();
        _config = config ?? new ContextWindowConfig();
        _metadata = new ContextMetadata();

        var ttl = TimeSpan.FromMinutes(_config.CacheTTLMinutes);
        _cache = _config.CacheEnabled ? new ContextCache(ttl) : new NullContextCache();

        _buffer = new SlidingWindowBuffer(logger, _config.MaxLines);

        _metadata.StartNewSession();

        _logger.LogInformation($"ContextManager initialized with MaxLines={_config.MaxLines}, CacheEnabled={_config.CacheEnabled}");
    }

    /// <summary>
    /// Gets the current terminal output context for LLM processing
    /// </summary>
    /// <param name="maxLines">Maximum number of terminal lines to include (default: 100)</param>
    /// <returns>Formatted string with terminal output lines</returns>
    public string GetContext(int maxLines = 100)
    {
        var cacheKey = $"context_{maxLines}";

        if (_config.CacheEnabled && _cache.IsAvailable(cacheKey))
        {
            var cached = _cache.Retrieve(cacheKey);
            if (cached != null)
            {
                _logger.LogTrace($"Retrieved context from cache (key: {cacheKey})");
                return cached;
            }
        }

        var lines = _buffer.GetLastNLines(maxLines);
        var context = string.Join('\n', lines);

        if (_config.CacheEnabled)
        {
            _cache.Store(cacheKey, context);
        }

        UpdateMetadata(lines, context);

        _logger.LogTrace($"Retrieved {lines.Length} lines, estimated {CountTokens(context)} tokens");
        return context;
    }

    /// <summary>
    /// Gets context with default max lines
    /// </summary>
    /// <returns>Formatted terminal output context string</returns>
    public string GetContext()
    {
        return GetContext(_config.MaxLines);
    }

    /// <summary>
    /// Gets estimated token count for the context
    /// </summary>
    /// <returns>Estimated number of tokens in context</returns>
    public int GetEstimatedTokenCount()
    {
        var context = GetContext();
        return CountTokens(context);
    }

    /// <summary>
    /// Gets the total character count of all context
    /// </summary>
    /// <returns>Total character count</returns>
    public int GetTotalCharacterCount()
    {
        return _buffer.GetTotalCharacterCount();
    }

    /// <summary>
    /// Gets the total line count of all context
    /// </summary>
    /// <returns>Total line count</returns>
    public int GetTotalLineCount()
    {
        return _buffer.LineCount;
    }

    /// <summary>
    /// Clears the context cache and buffer (for new session)
    /// </summary>
    public void Clear()
    {
        _buffer.Clear();
        _cache.ClearAll();
        _metadata.StartNewSession();
        _logger.LogInformation("Context cleared, new session started");
    }

    /// <summary>
    /// Updates the maximum context window size
    /// </summary>
    /// <param name="maxLines">New maximum number of terminal lines</param>
    public void SetMaxLines(int maxLines)
    {
        if (maxLines < _config.MinLines)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLines), $"MaxLines must be at least {_config.MinLines}");
        }

        _config.MaxLines = maxLines;
        Invalidate();
        _logger.LogInformation($"MaxLines updated to {maxLines}");
    }

    /// <summary>
    /// Enables or disables context caching
    /// </summary>
    /// <param name="enableCache">Whether to cache context</param>
    public void SetCacheEnabled(bool enableCache)
    {
        _config.CacheEnabled = enableCache;
        if (enableCache)
        {
            var ttl = TimeSpan.FromMinutes(_config.CacheTTLMinutes);
            _cache = new ContextCache(ttl);
        }
        else
        {
            _cache = new NullContextCache();
        }
        _logger.LogInformation($"Cache enabled: {enableCache}");
    }

    /// <summary>
    /// Gets current cache enabled status
    /// </summary>
    /// <returns>Current cache enabled status</returns>
    public bool IsCacheEnabled()
    {
        return _config.CacheEnabled;
    }

    /// <summary>
    /// Gets current max lines setting
    /// </summary>
    /// <returns>Current max lines setting</returns>
    public int GetMaxLines()
    {
        return _config.MaxLines;
    }

    /// <summary>
    /// Invalidates the cache (forces refresh on next GetContext)
    /// </summary>
    public void Invalidate()
    {
        _cache.ClearAll();
        _logger.LogTrace("Cache invalidated");
    }

    /// <summary>
    /// Registers a terminal output line
    /// </summary>
    /// <param name="line">Terminal output line</param>
    public void AddLine(string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            _buffer.AddLine(line);
            _metadata.UpdateActivity();
            Invalidate();
        }
    }

    /// <summary>
    /// Saves a snapshot of current context
    /// </summary>
    /// <returns>Snapshot string</returns>
    public string SaveContextSnapshot()
    {
        var snapshot = GetContext();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return $"[{timestamp}] Context Snapshot:\n{snapshot}";
    }

    /// <summary>
    /// Gets current statistics
    /// </summary>
    /// <returns>ContextStatistics object</returns>
    public ContextStatistics GetStatistics()
    {
        return new ContextStatistics
        {
            LineCount = GetTotalLineCount(),
            TokenCount = GetEstimatedTokenCount(),
            CacheHitCount = _cache.HitCount,
            CacheMissCount = _cache.MissCount,
            CacheHitRate = _cache.HitRate,
            SessionDuration = _metadata.GetSessionDuration(),
            IdleTime = _metadata.GetIdleTime()
        };
    }

    /// <summary>
    /// Counts tokens in text
    /// </summary>
    /// <param name="text">Text to count</param>
    /// <returns>Token count</returns>
    private int CountTokens(string text)
    {
        return _tokenCounter.CountTokens(text);
    }

    /// <summary>
    /// Updates metadata with current context info
    /// </summary>
    /// <param name="lines">Context lines</param>
    /// <param name="context">Context string</param>
    private void UpdateMetadata(string[] lines, string context)
    {
        var tokenCount = CountTokens(context);
        _metadata.UpdateCounts(lines.Length, tokenCount);
    }

    /// <summary>
    /// Disposes of resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cache.ClearAll();
            _disposed = true;
        }
    }

    /// <summary>
    /// Null cache implementation for when caching is disabled
    /// </summary>
    private class NullContextCache : IContextCache
    {
        public void Store(string context) { }
        public void Store(string cacheKey, string context) { }
        public string? Retrieve(string cacheKey) => null;
        public void Invalidate(string? cacheKey) { }
        public void ClearAll() { }
        public bool IsAvailable(string cacheKey) => false;
        public CacheHitInfo? GetCacheHitInfo(string cacheKey) => null;
        public int HitCount => 0;
        public int MissCount => 0;
        public double HitRate => 0.0;
        public int Count => 0;
    }
}
