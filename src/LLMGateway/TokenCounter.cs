using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PairAdmin.LLMGateway;

/// <summary>
/// Tokenizer type
/// </summary>
public enum TokenizerType
{
    GPT35,        // GPT-3.5 tokenizer (~4 chars/token)
    GPT4,         // GPT-4 tokenizer
    Claude,        // Claude tokenizer (~4 chars/token)
    Local,         // Local models (qwen, deepseek, etc.)
    Heuristic      // Fallback (~4 chars/token)
}

/// <summary>
/// Token count result
/// </summary>
public class TokenCountResult
{
    /// <summary>
    /// Total token count
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Context tokens
    /// </summary>
    public int ContextTokens { get; set; }

    /// <summary>
    /// System tokens
    /// </summary>
    public int SystemTokens { get; set; }

    /// <summary>
    /// Percentage of max context
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Tokenizer used
    /// </summary>
    public TokenizerType Tokenizer { get; set; }

    /// <summary>
    /// Gets the usage status
    /// </summary>
    public MeterStatus GetStatus()
    {
        if (Percentage >= 1.0)
        {
            return MeterStatus.OverLimit;
        }

        return Percentage switch
        {
            >= 0.90 => MeterStatus.Critical,
            >= 0.70 => MeterStatus.Warning,
            _ => MeterStatus.Safe
        };
    }
}

/// <summary>
/// Token counting service with support for multiple tokenizers
/// </summary>
public class TokenCounter
{
    private readonly ConcurrentDictionary<string, int> _cache;
    private TokenizerType _defaultTokenizer;
    private readonly ILogger<TokenCounter> _logger;

    /// <summary>
    /// Tokenizer character ratios (chars per token)
    /// </summary>
    private static readonly Dictionary<TokenizerType, double> TokenizerRatios = new()
    {
        { TokenizerType.GPT35, 4.0 },
        { TokenizerType.GPT4, 3.5 },
        { TokenizerType.Claude, 4.0 },
        { TokenizerType.Local, 4.0 },
        { TokenizerType.Heuristic, 4.0 }
    };

    /// <summary>
    /// Cache entry
    /// </summary>
    private class CacheEntry
    {
        public int TokenCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Token cache with TTL
    /// </summary>
    private readonly ConcurrentDictionary<string, CacheEntry> _tokenCache;
    private const int CacheTTLMinutes = 10;

    /// <summary>
    /// Initializes a new instance of TokenCounter
    /// </summary>
    public TokenCounter(
        ILogger<TokenCounter>? logger = null,
        TokenizerType defaultTokenizer = TokenizerType.Heuristic)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TokenCounter>.Instance;
        _defaultTokenizer = defaultTokenizer;
        _cache = new ConcurrentDictionary<string, int>();
        _tokenCache = new ConcurrentDictionary<string, CacheEntry>();

        _logger.LogInformation($"TokenCounter initialized with {defaultTokenizer}");
    }

    /// <summary>
    /// Counts tokens in text using default tokenizer
    /// </summary>
    /// <param name="text">Text to count</param>
    /// <returns>Token count</returns>
    public int CountTokens(string text)
    {
        return CountTokens(text, _defaultTokenizer);
    }

    /// <summary>
    /// Counts tokens in text using specified tokenizer
    /// </summary>
    /// <param name="text">Text to count</param>
    /// <param name="tokenizer">Tokenizer type to use</param>
    /// <returns>Token count</returns>
    public int CountTokens(string text, TokenizerType tokenizer)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var cacheKey = $"{tokenizer}_{text.GetHashCode()}";

        if (_tokenCache.TryGetValue(cacheKey, out var cached))
        {
            var age = DateTime.Now - cached.Timestamp;
            if (age.TotalMinutes < CacheTTLMinutes)
            {
                _logger.LogTrace($"Token count cache hit: {cached.TokenCount}");
                return cached.TokenCount;
            }
        }

        var ratio = TokenizerRatios.TryGetValue(tokenizer, out var r) ? r : 4.0;
        var count = (int)Math.Ceiling(text.Length / ratio);

        _tokenCache.AddOrUpdate(cacheKey, new CacheEntry
        {
            TokenCount = count,
            Timestamp = DateTime.Now
        }, (k, v) => v);

        _logger.LogTrace($"Counted {count} tokens for {text.Length} chars using {tokenizer}");

        return count;
    }

    /// <summary>
    /// Counts tokens in messages
    /// </summary>
    /// <param name="messages">Messages to count</param>
    /// <param name="tokenizer">Tokenizer type</param>
    /// <returns>Token count result</returns>
    public TokenCountResult CountMessages(
        System.Collections.Generic.IList<Models.Message> messages,
        TokenizerType? tokenizer = null)
    {
        var effectiveTokenizer = tokenizer ?? _defaultTokenizer;

        var contextTokens = messages.Sum(m => CountTokens(m.Content, effectiveTokenizer));
        var systemTokens = messages.Where(m => m.Role == Models.MessageRole.System)
                                      .Sum(m => CountTokens(m.Content, effectiveTokenizer));

        return new TokenCountResult
        {
            TotalTokens = contextTokens,
            ContextTokens = contextTokens,
            SystemTokens = systemTokens,
            Tokenizer = effectiveTokenizer
        };
    }

    /// <summary>
    /// Gets token count result with percentage
    /// </summary>
    /// <param name="tokens">Token count</param>
    /// <param name="maxTokens">Maximum tokens allowed</param>
    /// <param name="tokenizer">Tokenizer used</param>
    /// <returns>Token count result with percentage</returns>
    public TokenCountResult GetCountResult(
        int tokens,
        int maxTokens,
        TokenizerType? tokenizer = null)
    {
        var effectiveTokenizer = tokenizer ?? _defaultTokenizer;
        var percentage = maxTokens > 0 ? (double)tokens / maxTokens : 0.0;

        return new TokenCountResult
        {
            TotalTokens = tokens,
            ContextTokens = tokens,
            Percentage = percentage,
            Tokenizer = effectiveTokenizer
        };
    }

    /// <summary>
    /// Clears token cache
    /// </summary>
    public void ClearCache()
    {
        _tokenCache.Clear();
        _logger.LogInformation("Token cache cleared");
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    /// <returns>Cache entry count</returns>
    public int GetCacheSize()
    {
        return _tokenCache.Count;
    }

    /// <summary>
    /// Gets tokenizer ratio for a type
    /// </summary>
    /// <param name="tokenizer">Tokenizer type</param>
    /// <returns>Characters per token ratio</returns>
    public double GetTokenizerRatio(TokenizerType tokenizer)
    {
        return TokenizerRatios.TryGetValue(tokenizer, out var ratio) ? ratio : 4.0;
    }

    /// <summary>
    /// Gets the default tokenizer type
    /// </summary>
    /// <returns>Default tokenizer type</returns>
    public TokenizerType GetDefaultTokenizer()
    {
        return _defaultTokenizer;
    }

    /// <summary>
    /// Sets the default tokenizer type
    /// </summary>
    /// <param name="tokenizer">New default tokenizer</param>
    public void SetDefaultTokenizer(TokenizerType tokenizer)
    {
        _defaultTokenizer = tokenizer;
        _logger.LogInformation($"Default tokenizer set to {tokenizer}");
    }

    /// <summary>
    /// Estimates tokens for a list of texts
    /// </summary>
    /// <param name="texts">List of texts</param>
    /// <param name="tokenizer">Tokenizer type</param>
    /// <returns>Total token count</returns>
    public int EstimateTokens(
        System.Collections.Generic.IList<string> texts,
        TokenizerType? tokenizer = null)
    {
        var effectiveTokenizer = tokenizer ?? _defaultTokenizer;
        return texts.Sum(t => CountTokens(t, effectiveTokenizer));
    }

    /// <summary>
    /// Gets token count for context and system prompt combined
    /// </summary>
    /// <param name="context">Context text</param>
    /// <param name="systemPrompt">System prompt</param>
    /// <param name="tokenizer">Tokenizer type</param>
    /// <returns>Combined token count</returns>
    public int CountContextAndSystem(
        string context,
        string systemPrompt,
        TokenizerType? tokenizer = null)
    {
        var effectiveTokenizer = tokenizer ?? _defaultTokenizer;
        var contextTokens = CountTokens(context, effectiveTokenizer);
        var systemTokens = CountTokens(systemPrompt, effectiveTokenizer);

        _logger.LogTrace($"Context: {contextTokens} tokens, System: {systemTokens} tokens");

        return contextTokens + systemTokens;
    }

    /// <summary>
    /// Gets recommended max tokens for a model
    /// </summary>
    /// <param name="model">Model name</param>
    /// <returns>Recommended max tokens</returns>
    public int GetRecommendedMaxTokens(string model)
    {
        return model.ToLower() switch
        {
            var m when m.Contains("gpt-4") => 8192,
            var m when m.Contains("gpt-3") => 4096,
            var m when m.Contains("claude") => 200000,
            var m when m.Contains("qwen") || m.Contains("deepseek") => 4096,
            _ => 4096
        };
    }

    /// <summary>
    /// Gets meter status based on percentage
    /// </summary>
    /// <param name="percentage">Percentage of max context</param>
    /// <returns>Meter status</returns>
    public MeterStatus GetMeterStatus(double percentage)
    {
        if (percentage >= 1.0)
        {
            return MeterStatus.OverLimit;
        }

        return percentage switch
        {
            >= 0.90 => MeterStatus.Critical,
            >= 0.70 => MeterStatus.Warning,
            _ => MeterStatus.Safe
        };
    }
}

/// <summary>
/// Meter status for context usage
/// </summary>
public enum MeterStatus
{
    Safe,       // Green (< 70%)
    Warning,    // Yellow (70-90%)
    Critical,   // Red (> 90%)
    OverLimit   // Over 100%
}
