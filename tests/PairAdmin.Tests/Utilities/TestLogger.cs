using Microsoft.Extensions.Logging;

namespace PairAdmin.Tests.Utilities;

/// <summary>
/// Test logger that captures log entries
/// </summary>
public class TestLogger<T> : ILogger<T>, IDisposable
{
    private readonly List<LogEntry> _entries = new();
    private readonly ILogger<T>? _innerLogger;
    private readonly LogLevel _minLevel;

    public IReadOnlyList<LogEntry> Entries => _entries;

    public TestLogger(LogLevel minLevel = LogLevel.Debug)
    {
        _minLevel = minLevel;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return _innerLogger?.BeginScope(state) ?? NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLevel && (_innerLogger?.IsEnabled(logLevel) ?? true);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel < _minLevel)
            return;

        var message = formatter(state, exception);
        _entries.Add(new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel,
            Message = message,
            Exception = exception,
            Category = typeof(T).Name,
            EventId = eventId.Name
        });

        _innerLogger?.Log(logLevel, eventId, state, exception, formatter);
    }

    /// <summary>
    /// Sets the inner logger
    /// </summary>
    public void SetInnerLogger(ILogger<T>? logger)
    {
        _innerLogger = logger;
    }

    /// <summary>
    /// Clears all captured entries
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    /// Gets entries by log level
    /// </summary>
    public IEnumerable<LogEntry> GetByLevel(LogLevel level)
    {
        return _entries.Where(e => e.Level == level);
    }

    /// <summary>
    /// Checks if an entry with the given message exists
    /// </summary>
    public bool HasMessage(string message, LogLevel? level = null)
    {
        return _entries.Any(e =>
            e.Message.Contains(message, StringComparison.OrdinalIgnoreCase) &&
            (!level.HasValue || e.Level == level.Value));
    }

    public void Dispose()
    {
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? EventId { get; set; }
}

public static class NullScope
{
    public static IDisposable Instance { get; } = new Disposable();

    private class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
