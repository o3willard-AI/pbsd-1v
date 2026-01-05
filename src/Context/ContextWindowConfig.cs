namespace PairAdmin.Context;

/// <summary>
/// Configuration settings for context window management
/// </summary>
public class ContextWindowConfig
{
    /// <summary>
    /// Default maximum number of lines in context window
    /// </summary>
    public int DefaultMaxLines { get; } = 100;

    /// <summary>
    /// Minimum allowed max lines
    /// </summary>
    public int MinMaxLines { get; } = 10;

    /// <summary>
    /// Default max lines as percentage (1.0 = 100%)
    /// </summary>
    public double DefaultMaxLinesPercentage { get; } = 1.0;

    /// <summary>
    /// Default cache enabled status
    /// </summary>
    public bool DefaultCacheEnabled { get; } = true;

    /// <summary>
    /// Default cache time-to-live in minutes
    /// </summary>
    public double DefaultCacheTTLMinutes { get; } = 5.0;

    /// <summary>
    /// Default token estimation ratio (characters per token)
    /// </summary>
    public double DefaultTokenEstimationRatio { get; } = 4.0;

    /// <summary>
    /// Current maximum number of lines in context window
    /// </summary>
    public int MaxLines { get; set; }

    /// <summary>
    /// Current minimum allowed max lines
    /// </summary>
    public int MinLines { get; set; }

    /// <summary>
    /// Current max lines as percentage (0.0 to 1.0)
    /// </summary>
    public double MaxLinesPercentage { get; set; }

    /// <summary>
    /// Whether context caching is enabled
    /// </summary>
    public bool CacheEnabled { get; set; }

    /// <summary>
    /// Cache time-to-live in minutes
    /// </summary>
    public double CacheTTLMinutes { get; set; }

    /// <summary>
    /// Token estimation ratio (characters per token)
    /// </summary>
    public double TokenEstimationRatio { get; set; }

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public ContextWindowConfig()
    {
        MaxLines = DefaultMaxLines;
        MinLines = MinMaxLines;
        MaxLinesPercentage = DefaultMaxLinesPercentage;
        CacheEnabled = DefaultCacheEnabled;
        CacheTTLMinutes = DefaultCacheTTLMinutes;
        TokenEstimationRatio = DefaultTokenEstimationRatio;
    }

    /// <summary>
    /// Validates the configuration settings
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool Validate()
    {
        if (MaxLines < MinLines)
        {
            return false;
        }

        if (MaxLinesPercentage < 0.0 || MaxLinesPercentage > 1.0)
        {
            return false;
        }

        if (CacheTTLMinutes < 0)
        {
            return false;
        }

        if (TokenEstimationRatio <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a copy of this configuration
    /// </summary>
    /// <returns>New ContextWindowConfig instance</returns>
    public ContextWindowConfig Clone()
    {
        return new ContextWindowConfig
        {
            MaxLines = this.MaxLines,
            MinLines = this.MinLines,
            MaxLinesPercentage = this.MaxLinesPercentage,
            CacheEnabled = this.CacheEnabled,
            CacheTTLMinutes = this.CacheTTLMinutes,
            TokenEstimationRatio = this.TokenEstimationRatio
        };
    }
}
