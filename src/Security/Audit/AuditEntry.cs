using System.Text.Json.Serialization;

namespace PairAdmin.Security.Audit;

/// <summary>
/// Represents an audit log entry
/// </summary>
public class AuditEntry
{
    /// <summary>
    /// Unique entry ID
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When the event occurred (UTC)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of event
    /// </summary>
    [JsonPropertyName("event_type")]
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// Event category
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Related command (if applicable)
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    /// <summary>
    /// Event result
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// User or context identifier
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }

    /// <summary>
    /// Additional details
    /// </summary>
    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Duration of operation in milliseconds
    /// </summary>
    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; set; }

    /// <summary>
    /// Extra metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Session ID
    /// </summary>
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// IP address (if available)
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Creates a summary string
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>
        {
            $"[{Timestamp:HH:mm:ss}]",
            EventType.ToString(),
            Category
        };

        if (!string.IsNullOrEmpty(Command))
        {
            parts.Add(Command);
        }

        if (!string.IsNullOrEmpty(Result))
        {
            parts.Add($"- {Result}");
        }

        return string.Join(" | ", parts);
    }
}
