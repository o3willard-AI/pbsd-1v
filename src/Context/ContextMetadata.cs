namespace PairAdmin.Context;

/// <summary>
/// Metadata for context sessions
/// Tracks session information, statistics, and timestamps
/// </summary>
public class ContextMetadata
{
    /// <summary>
    /// Session start time
    /// </summary>
    public DateTime SessionStartTime { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivityTime { get; set; }

    /// <summary>
    /// Total number of lines in context
    /// </summary>
    public int LineCount { get; set; }

    /// <summary>
    /// Total number of tokens in context
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Estimated token count (calculated)
    /// </summary>
    public double EstimatedTokenCount { get; set; }

    /// <summary>
    /// Session ID for tracking
    /// </summary>
    public string SessionId { get; set; }

    /// <summary>
    /// User ID for tracking
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets the session duration
    /// </summary>
    /// <returns>TimeSpan from start to now or last activity</returns>
    public TimeSpan GetSessionDuration()
    {
        return DateTime.Now - SessionStartTime;
    }

    /// <summary>
    /// Gets the idle time since last activity
    /// </summary>
    /// <returns>TimeSpan from last activity to now</returns>
    public TimeSpan GetIdleTime()
    {
        return DateTime.Now - LastActivityTime;
    }

    /// <summary>
    /// Updates the last activity timestamp to now
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityTime = DateTime.Now;
    }

    /// <summary>
    /// Updates line and token counts
    /// </summary>
    /// <param name="lineCount">New line count</param>
    /// <param name="tokenCount">New token count</param>
    public void UpdateCounts(int lineCount, int tokenCount)
    {
        LineCount = lineCount;
        TokenCount = tokenCount;
        EstimatedTokenCount = tokenCount;
        UpdateActivity();
    }

    /// <summary>
    /// Creates a new session with current timestamp
    /// </summary>
    public void StartNewSession()
    {
        SessionStartTime = DateTime.Now;
        LastActivityTime = DateTime.Now;
        LineCount = 0;
        TokenCount = 0;
        EstimatedTokenCount = 0;
        SessionId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public ContextMetadata()
    {
        SessionStartTime = DateTime.Now;
        LastActivityTime = DateTime.Now;
        SessionId = Guid.NewGuid().ToString();
    }
}
