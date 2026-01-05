using System.Text.Json;
using System.Text.Json.Serialization;

namespace PairAdmin.Security.Audit;

/// <summary>
/// Types of security events
/// </summary>
public enum SecurityEventType
{
    /// <summary>
    /// User authentication attempt
    /// </summary>
    AuthenticationAttempt,

    /// <summary>
    /// Privilege level changed
    /// </summary>
    PrivilegeChanged,

    /// <summary>
    /// Security violation detected
    /// </summary>
    SecurityViolation,

    /// <summary>
    /// Suspicious activity detected
    /// </summary>
    SuspiciousActivity,

    /// <summary>
    /// Rate limit exceeded
    /// </summary>
    RateLimitExceeded
}

/// <summary>
/// Types of audit events
/// </summary>
public enum AuditEventType
{
    // Command events
    /// <summary>
    /// Command was executed
    /// </summary>
    CommandExecuted,

    /// <summary>
    /// Command was blocked
    /// </summary>
    CommandBlocked,

    /// <summary>
    /// Command validation failed
    /// </summary>
    CommandValidationFailed,

    // Security events
    /// <summary>
    /// Privilege level changed
    /// </summary>
    PrivilegeChanged,

    /// <summary>
    /// Authentication attempt
    /// </summary>
    AuthenticationAttempt,

    /// <summary>
    /// Security violation detected
    /// </summary>
    SecurityViolation,

    // Data events
    /// <summary>
    /// Sensitive data was detected
    /// </summary>
    SensitiveDataDetected,

    /// <summary>
    /// Data was filtered
    /// </summary>
    DataFiltered,

    /// <summary>
    /// Data was exported
    /// </summary>
    DataExported,

    // System events
    /// <summary>
    /// Application started
    /// </summary>
    ApplicationStarted,

    /// <summary>
    /// Application stopped
    /// </summary>
    ApplicationStopped,

    /// <summary>
    /// Configuration changed
    /// </summary>
    ConfigurationChanged,

    /// <summary>
    /// Error occurred
    /// </summary>
    ErrorOccurred
}

/// <summary>
/// Export format options
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// JSON format
    /// </summary>
    Json,

    /// <summary>
    /// CSV format
    /// </summary>
    Csv,

    /// <summary>
    /// Plain text format
    /// </summary>
    Text
}

/// <summary>
/// Parameters for querying audit entries
/// </summary>
public class QueryParameters
{
    /// <summary>
    /// Start time filter
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time filter
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Event types to include
    /// </summary>
    public HashSet<AuditEventType> EventTypes { get; set; } = new();

    /// <summary>
    /// Command name filter
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Category filter
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Result filter (success/failure)
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Maximum results to return
    /// </summary>
    public int Limit { get; set; } = 1000;

    /// <summary>
    /// Skip entries (for pagination)
    /// </summary>
    public int Offset { get; set; } = 0;
}
