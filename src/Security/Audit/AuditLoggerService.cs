using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Security.Audit;

/// <summary>
/// Service for audit logging
/// </summary>
public class AuditLoggerService : IAuditLogger
{
    private readonly ConcurrentQueue<AuditEntry> _inMemoryEntries;
    private readonly string _logFilePath;
    private readonly ILogger<AuditLoggerService>? _logger;
    private readonly string _sessionId;
    private readonly int _maxInMemoryEntries;
    private readonly SemaphoreSlim _writeSemaphore;

    private const int DefaultMaxInMemoryEntries = 10000;

    /// <summary>
    /// Creates a default audit logger
    /// </summary>
    public static AuditLoggerService CreateDefault(ILogger<AuditLoggerService>? logger = null)
    {
        return new AuditLoggerService(logger: logger);
    }

    /// <summary>
    /// Creates an audit logger with custom settings
    /// </summary>
    public AuditLoggerService(
        string? logFilePath = null,
        ILogger<AuditLoggerService>? logger = null,
        int maxInMemoryEntries = DefaultMaxInMemoryEntries)
    {
        _inMemoryEntries = new ConcurrentQueue<AuditEntry>();
        _logger = logger;
        _sessionId = Guid.NewGuid().ToString("N")[..8];
        _maxInMemoryEntries = maxInMemoryEntries;
        _writeSemaphore = new SemaphoreSlim(1, 1);

        _logFilePath = logFilePath ?? GetDefaultLogPath();
        EnsureLogDirectory();

        LogApplicationEvent(AuditEventType.ApplicationStarted, "Audit logger initialized");

        _logger?.LogInformation("AuditLoggerService initialized. Log path: {Path}", _logFilePath);
    }

    public void LogCommand(
        string command,
        string result,
        TimeSpan duration,
        bool success,
        string? user = null)
    {
        var entry = new AuditEntry
        {
            EventType = success ? AuditEventType.CommandExecuted : AuditEventType.CommandBlocked,
            Category = "Command",
            Command = command,
            Result = result,
            DurationMs = (long)duration.TotalMilliseconds,
            User = user,
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
            EventType = AuditEventType.CommandValidationFailed,
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
        string context,
        bool filtered)
    {
        var entry = new AuditEntry
        {
            EventType = AuditEventType.SensitiveDataDetected,
            Category = "Data",
            Details = $"Pattern: {patternName}",
            Result = filtered ? "Filtered" : "Detected",
            Metadata = new Dictionary<string, object>
            {
                ["Pattern"] = patternName,
                ["ContextLength"] = context.Length
            },
            SessionId = _sessionId
        };

        Enqueue(entry);
    }

    public void LogApplicationEvent(
        AuditEventType eventType,
        string details)
    {
        var entry = new AuditEntry
        {
            EventType = eventType,
            Category = "Application",
            Details = details,
            SessionId = _sessionId
        };

        Enqueue(entry);
    }

    public void LogError(
        string context,
        string errorMessage,
        string? stackTrace = null)
    {
        var entry = new AuditEntry
        {
            EventType = AuditEventType.ErrorOccurred,
            Category = "Error",
            Details = $"{context}: {errorMessage}",
            Metadata = new Dictionary<string, object>
            {
                ["Context"] = context,
                ["StackTrace"] = stackTrace ?? "N/A"
            },
            SessionId = _sessionId
        };

        Enqueue(entry);
    }

    public IEnumerable<AuditEntry> Query(QueryParameters parameters)
    {
        var entries = _inMemoryEntries
            .Where(e =>
            {
                if (parameters.StartTime.HasValue && e.Timestamp < parameters.StartTime.Value)
                    return false;

                if (parameters.EndTime.HasValue && e.Timestamp > parameters.EndTime.Value)
                    return false;

                if (parameters.EventTypes.Count > 0 &&
                    !parameters.EventTypes.Contains(e.EventType))
                    return false;

                if (!string.IsNullOrEmpty(parameters.Command) &&
                    !string.Equals(e.Command, parameters.Command, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(parameters.Category) &&
                    !string.Equals(e.Category, parameters.Category, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(parameters.Result) &&
                    !string.Equals(e.Result, parameters.Result, StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            })
            .OrderByDescending(e => e.Timestamp)
            .Skip(parameters.Offset);

        if (parameters.Limit > 0)
        {
            entries = entries.Take(parameters.Limit);
        }

        return entries;
    }

    public int GetEntryCount() => _inMemoryEntries.Count;

    public async Task ExportAsync(
        string path,
        ExportFormat format,
        QueryParameters? parameters = null)
    {
        var entries = (parameters != null ? Query(parameters) : _inMemoryEntries.ToList())
            .OrderBy(e => e.Timestamp)
            .ToList();

        await _writeSemaphore.WaitAsync();
        try
        {
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

            _logger?.LogInformation("Exported {Count} audit entries to {Path}", entries.Count, path);
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    private void Enqueue(AuditEntry entry)
    {
        _inMemoryEntries.Enqueue(entry);

        Task.Run(() => PersistEntry(entry));

        while (_inMemoryEntries.Count > _maxInMemoryEntries)
        {
            _inMemoryEntries.TryDequeue(out _);
        }
    }

    private async void PersistEntry(AuditEntry entry)
    {
        await _writeSemaphore.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false
            };

            var line = JsonSerializer.Serialize(entry, options);
            await File.AppendAllTextAsync(_logFilePath, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist audit entry");
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    private async Task ExportJsonAsync(FileStream stream, List<AuditEntry> entries)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(new
        {
            ExportedAt = DateTime.UtcNow,
            EntryCount = entries.Count,
            Entries = entries
        }, options);

        var bytes = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(bytes);
    }

    private async Task ExportCsvAsync(FileStream stream, List<AuditEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,EventType,Category,Command,Result,User,Details,DurationMs");

        foreach (var entry in entries)
        {
            sb.AppendLine($"{entry.Timestamp:O},{entry.EventType},{entry.Category}," +
                $"{EscapeCsv(entry.Command)},{EscapeCsv(entry.Result)},{EscapeCsv(entry.User)}," +
                $"{EscapeCsv(entry.Details)},{entry.DurationMs}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await stream.WriteAsync(bytes);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var escaped = value.Replace("\"", "\"\"");
        if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n'))
        {
            escaped = $"\"{escaped}\"";
        }
        return escaped;
    }

    private async Task ExportTextAsync(FileStream stream, List<AuditEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Audit Log Export ===");
        sb.AppendLine($"Exported: {DateTime.UtcNow:O}");
        sb.AppendLine($"Entries: {entries.Count}");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.AppendLine(entry.GetSummary());
            if (!string.IsNullOrEmpty(entry.Details))
            {
                sb.AppendLine($"  Details: {entry.Details}");
            }
            if (entry.DurationMs > 0)
            {
                sb.AppendLine($"  Duration: {entry.DurationMs}ms");
            }
            sb.AppendLine();
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        await stream.WriteAsync(bytes);
    }

    private void EnsureLogDirectory()
    {
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string GetDefaultLogPath()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pairAdminPath = Path.Combine(basePath, "PairAdmin");

        var logsPath = Path.Combine(pairAdminPath, "Logs");
        if (!Directory.Exists(logsPath))
        {
            Directory.CreateDirectory(logsPath);
        }

        return Path.Combine(logsPath, "audit.jsonl");
    }

    public void Dispose()
    {
        LogApplicationEvent(AuditEventType.ApplicationStopped, "Audit logger disposed");
        _writeSemaphore.Dispose();
    }
}
