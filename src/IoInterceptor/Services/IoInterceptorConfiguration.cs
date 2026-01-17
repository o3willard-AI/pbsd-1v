namespace PairAdmin.IoInterceptor.Services;

/// <summary>
/// Configuration settings for the I/O Interceptor
/// </summary>
public class IoInterceptorConfiguration
{
    /// <summary>
    /// Whether to enable data filtering for sensitive content
    /// </summary>
    public bool EnableFiltering { get; set; } = true;

    /// <summary>
    /// Whether to filter password prompts
    /// </summary>
    public bool FilterPasswords { get; set; } = true;

    /// <summary>
    /// Whether to filter private keys
    /// </summary>
    public bool FilterPrivateKeys { get; set; } = true;

    /// <summary>
    /// Whether to filter API keys
    /// </summary>
    public bool FilterApiKeys { get; set; } = true;

    /// <summary>
    /// Maximum events to keep in memory buffer
    /// </summary>
    public int MaxEventsInMemory { get; set; } = 1000;

    /// <summary>
    /// Buffer size for event data (bytes)
    /// </summary>
    public int BufferSize { get; set; } = 8192;

    /// <summary>
    /// Whether to track output event statistics
    /// </summary>
    public bool TrackOutputEvents { get; set; } = true;

    /// <summary>
    /// Whether to track input event statistics
    /// </summary>
    public bool TrackInputEvents { get; set; } = true;

    /// <summary>
    /// Custom patterns for filtering (regex strings)
    /// </summary>
    public List<string> CustomFilterPatterns { get; set; } = new();
}
