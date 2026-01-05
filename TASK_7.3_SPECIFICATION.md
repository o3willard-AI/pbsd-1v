# Task 7.3 Specification: Audit Logging

## Task: Implement Audit Logging Service

**Phase:** Phase 7: Security  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** None

---

## Description

Implement a comprehensive audit logging service that records all security-relevant events for compliance, debugging, and security analysis. This includes command executions, validation failures, sensitive data detections, and system events.

---

## Deliverables

### 1. IAuditLogger.cs
Audit logging interface:
- Log event methods
- Query methods
- Event types

### 2. AuditEntry.cs
Audit event model:
- Event data structure
- Serialization support

### 3. AuditLoggerService.cs
Core logging service:
- Event persistence
- Query capabilities
- Export functionality

### 4. AuditEventTypes.cs
Event type definitions:
- Command execution
- Validation events
- Security events
- System events

---

## Requirements

### Functional Requirements

#### Event Types
| Category | Events |
|----------|--------|
| Command | Execution, validation failure, blocked |
| Security | Privilege changes, authentication |
| Data | Sensitive data detected, filtering |
| System | Startup, shutdown, configuration changes |

#### Event Data
| Field | Description |
|-------|-------------|
| Timestamp | When the event occurred |
| EventType | Category of event |
| Command | Related command (if applicable) |
| User | Current user context |
| Result | Success/failure status |
| Details | Additional information |
| Metadata | Extra event data |

#### Query Capabilities
| Requirement | Description |
|-------------|-------------|
| By Time Range | Filter by date/time |
| By Event Type | Filter by category |
| By Command | Filter by command name |
| By Result | Filter by success/failure |
| Export | Export to file |

### Non-Functional Requirements

1. **Performance**
   - Non-blocking writes
   - Efficient storage
   - Query performance

2. **Security**
   - Immutable logs
   - Tamper detection
   - Access control

3. **Reliability**
   - Persistent storage
   - Recovery support
   - Rotation policies

---

## Implementation

### IAuditLogger Interface

```csharp
namespace PairAdmin.Security.Audit;

/// <summary>
/// Interface for audit logging
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs a command execution
    /// </summary>
    void LogCommand(
        string command,
        string result,
        TimeSpan duration,
        bool success);

    /// <summary>
    /// Logs a security event
    /// </summary>
    void LogSecurity(
        SecurityEventType eventType,
        string details,
        string? user = null);

    /// <summary>
    /// Logs a validation event
    /// </summary>
    void LogValidation(
        string command,
        string reason,
        bool blocked);

    /// <summary>
    /// Logs sensitive data detection
    /// </summary>
    void LogSensitiveData(
        string patternName,
        string context);

    /// <summary>
    /// Queries audit entries
    /// </summary>
    IEnumerable<AuditEntry> Query(QueryParameters parameters);

    /// <summary>
    /// Exports audit log
    /// </summary>
    Task ExportAsync(string path, ExportFormat format);
}
```

### AuditEntry Model

```csharp
namespace PairAdmin.Security.Audit;

/// <summary>
/// Represents an audit log entry
/// </summary>
public class AuditEntry
{
    /// <summary>
    /// Unique entry ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of event
    /// </summary>
    public AuditEventType EventType { get; set; }

    /// <summary>
    /// Event category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Related command (if applicable)
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Event result
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// User or context
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Additional details
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Duration of operation
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Extra metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Session ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}
```

### AuditEventTypes

```csharp
namespace PairAdmin.Security.Audit;

/// <summary>
/// Types of audit events
/// </summary>
public enum AuditEventType
{
    // Command events
    CommandExecuted,
    CommandBlocked,
    CommandValidationFailed,

    // Security events
    PrivilegeChanged,
    AuthenticationAttempt,
    SecurityViolation,

    // Data events
    SensitiveDataDetected,
    DataFiltered,
    DataExported,

    // System events
    ApplicationStarted,
    ApplicationStopped,
    ConfigurationChanged,
    ErrorOccurred
}
```

### AuditLoggerService

```csharp
namespace PairAdmin.Security.Audit;

/// <summary>
/// Service for audit logging
/// </summary>
public class AuditLoggerService : IAuditLogger
{
    private readonly ConcurrentQueue<AuditEntry> _entries;
    private readonly string _logPath;
    private readonly ILogger<AuditLoggerService>? _logger;
    private readonly string _sessionId;

    public AuditLoggerService(
        string? logPath = null,
        ILogger<AuditLoggerService>? logger = null)
    {
        _entries = new ConcurrentQueue<AuditEntry>();
        _logger = logger;
        _sessionId = Guid.NewGuid().ToString("N")[..8];
        _logPath = logPath ?? GetDefaultLogPath();

        Directory.CreateDirectory(Path.GetDirectoryName(_logPath) ?? ".");
        _logger?.LogInformation("Audit logger initialized: {Path}", _logPath);
    }

    public void LogCommand(
        string command,
        string result,
        TimeSpan duration,
        bool success)
    {
        var entry = new AuditEntry
        {
            EventType = success ? AuditEventType.CommandExecuted : AuditEventType.CommandBlocked,
            Category = "Command",
            Command = command,
            Result = result,
            DurationMs = (long)duration.TotalMilliseconds,
            SessionId = _sessionId
        };

        Enqueue(entry);
    }

    public void LogSecurity(
        SecurityEventType eventType,
        string details,
        string? user = null)
    {
        var entry = new AuditEntry
        {
            EventType = (AuditEventType)eventType,
            Category = "Security",
            Details = details,
            User = user,
            SessionId = _sessionId
        };

        Enqueue(entry);
    }

    public void LogValidation(
        string command,
        string reason,
        bool blocked)
    {
        var entry = new AuditEntry
        {
            EventType = blocked
                ? AuditEventType.CommandValidationFailed
                : AuditEventType.CommandBlocked,
            Category = "Validation",
            Command = command,
            Details = reason,
            Result = blocked ? "Blocked" : "Warning",
            SessionId = _sessionId
        };

        Enqueue(entry);
    }

    public void LogSensitiveData(
        string patternName,
        string context)
    {
        var entry = new AuditEntry
        {
            EventType = AuditEventType.SensitiveDataDetected,
            Category = "Data",
            Details = $"Pattern detected: {patternName}",
            Metadata = new Dictionary<string, object>
            {
                ["Pattern"] = patternName,
                ["ContextLength"] = context.Length
            },
            SessionId = _sessionId
        };

        Enqueue(entry);
    }

    public IEnumerable<AuditEntry> Query(QueryParameters parameters)
    {
        return _entries.Where(e =>
        {
            if (parameters.StartTime.HasValue && e.Timestamp < parameters.StartTime)
                return false;

            if (parameters.EndTime.HasValue && e.Timestamp > parameters.EndTime)
                return false;

            if (parameters.EventTypes.Count > 0 &&
                !parameters.EventTypes.Contains(e.EventType))
                return false;

            if (!string.IsNullOrEmpty(parameters.Command) &&
                e.Command != parameters.Command)
                return false;

            if (!string.IsNullOrEmpty(parameters.Category) &&
                e.Category != parameters.Category)
                return false;

            return true;
        }).OrderByDescending(e => e.Timestamp);
    }

    public async Task ExportAsync(string path, ExportFormat format)
    {
        var entries = _entries.ToList();

        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        switch (format)
        {
            case ExportFormat.Json:
                await ExportJsonAsync(stream, entries);
                break;
            case ExportFormat.Csv:
                await ExportCsvAsync(stream, entries);
                break;
            case ExportFormat.Text:
                await ExportTextAsync(stream, entries);
                break;
        }

        _logger?.LogInformation("Exported {Count} entries to {Path}", entries.Count, path);
    }

    private void Enqueue(AuditEntry entry)
    {
        _entries.Enqueue(entry);

        // Persistent write in background
        Task.Run(() => PersistEntry(entry));

        // Memory management
        while (_entries.Count > 10000)
        {
            _entries.TryDequeue(out _);
        }
    }

    private async void PersistEntry(AuditEntry entry)
    {
        try
        {
            var line = JsonSerializer.Serialize(entry);
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist audit entry");
        }
    }

    private static string GetDefaultLogPath()
    {
        var basePath = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);
        return Path.Combine(basePath, "PairAdmin", "Logs", "audit.jsonl");
    }
}
```

---

## Query Parameters

```csharp
namespace PairAdmin.Security.Audit;

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
    /// Maximum results
    /// </summary>
    public int Limit { get; set; } = 1000;
}

/// <summary>
/// Export format
/// </summary>
public enum ExportFormat
{
    Json,
    Csv,
    Text
}
```

---

## Integration Points

### With CommandDispatcher (Task 6.1)
```csharp
// After command execution
auditLogger.LogCommand(
    command,
    result.Response,
    duration,
    result.IsSuccess);
```

### With CommandValidationService (Task 7.2)
```csharp
// On validation failure
auditLogger.LogValidation(
    command,
    result.FailureReason!,
    blocked: true);
```

### With SensitiveDataFilter (Task 7.1)
```csharp
// On sensitive data detection
auditLogger.LogSensitiveData(
    pattern.Name,
    context);
```

---

## Error Handling

| Scenario | Handling |
|----------|----------|
| Log write failed | Log to fallback, continue |
| Query timeout | Return partial results |
| Export failed | Throw exception |
| Disk full | Circular buffer, alert |

---

## Testing

### Unit Tests

```csharp
[Fact]
public void LogCommand_StoresEntry()
{
    // Arrange
    var logger = new AuditLoggerService();
    var command = "/help";
    var duration = TimeSpan.FromMilliseconds(50);

    // Act
    logger.LogCommand(command, "Success", duration, true);
    var entries = logger.Query(new QueryParameters());

    // Assert
    Assert.Single(entries);
    Assert.Equal(AuditEventType.CommandExecuted, entries.First().EventType);
    Assert.Equal("/help", entries.First().Command);
}

[Fact]
public void Query_FiltersByTime()
{
    // Arrange
    var logger = new AuditLoggerService();
    logger.LogSecurity(SecurityEventType.Login, "User logged in");

    var startTime = DateTime.UtcNow.AddMinutes(-1);
    var parameters = new QueryParameters { StartTime = startTime };

    // Act
    var entries = logger.Query(parameters);

    // Assert
    Assert.Single(entries);
}
```

---

## Acceptance Criteria

- [ ] Logs command executions
- [ ] Logs validation failures
- [ ] Logs sensitive data detections
- [ ] Supports time-based queries
- [ ] Supports event type filters
- [ ] Exports to JSON/CSV/Text
- [ ] Non-blocking writes
- [ ] Persistent storage
- [ ] Session identification
- [ ] Performance metrics

---

## Files Created

```
src/Security/Audit/
├── IAuditLogger.cs              # Logger interface
├── AuditEntry.cs                # Entry model
├── AuditEventTypes.cs           # Event type enum
├── AuditLoggerService.cs        # Main service
├── QueryParameters.cs           # Query parameters
└── ExportFormat.cs              # Export format enum
```

---

## Estimated Complexity

| File | Complexity | Lines |
|------|------------|-------|
| IAuditLogger.cs | Low | ~50 |
| AuditEntry.cs | Low | ~80 |
| AuditEventTypes.cs | Low | ~30 |
| AuditLoggerService.cs | Medium | ~200 |
| QueryParameters.cs | Low | ~40 |
| ExportFormat.cs | Low | ~10 |

**Total Estimated:** ~410 lines of C#

---

## Next Steps

After Task 7.3 is complete:
1. Create PHASE_7_COMPLETE_SUMMARY.md
2. Document Phase 7 features

---

## Notes

- Use JSONL format for log persistence
- Add compression for old logs
- Consider log rotation
- Add integrity checking
- Support remote syslog
