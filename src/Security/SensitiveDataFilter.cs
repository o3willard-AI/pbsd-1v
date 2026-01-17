using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Security;

/// <summary>
/// Service for detecting and redacting sensitive data from text and messages
/// </summary>
public class SensitiveDataFilter
{
    private readonly List<IFilterPattern> _patterns;
    private readonly RedactionStrategy _defaultStrategy;
    private readonly ILogger<SensitiveDataFilter>? _logger;
    private bool _disposed;

    /// <summary>
    /// Gets the number of registered patterns
    /// </summary>
    public int PatternCount => _patterns.Count;

    /// <summary>
    /// Gets all registered patterns
    /// </summary>
    public IReadOnlyList<IFilterPattern> Patterns => _patterns.AsReadOnly();

    /// <summary>
    /// Initializes a new instance with default strategy
    /// </summary>
    public SensitiveDataFilter(
        RedactionStrategy defaultStrategy = RedactionStrategy.Mask,
        ILogger<SensitiveDataFilter>? logger = null)
    {
        _defaultStrategy = defaultStrategy;
        _logger = logger;
        _patterns = new List<IFilterPattern>();
    }

    /// <summary>
    /// Initializes with default patterns
    /// </summary>
    public static SensitiveDataFilter CreateWithDefaults(ILogger<SensitiveDataFilter>? logger = null)
    {
        var filter = new SensitiveDataFilter(logger: logger);
        foreach (var pattern in DefaultPatterns.GetAllPatterns())
        {
            filter.AddPattern(pattern);
        }
        return filter;
    }

    /// <summary>
    /// Adds a custom pattern
    /// </summary>
    public void AddPattern(IFilterPattern pattern)
    {
        _patterns.Add(pattern ?? throw new ArgumentNullException(nameof(pattern)));
        _logger?.LogDebug("Added filter pattern: {PatternName}", pattern.Name);
    }

    /// <summary>
    /// Removes a pattern by name
    /// </summary>
    public bool RemovePattern(string name)
    {
        var pattern = _patterns.FirstOrDefault(p => p.Name == name);
        if (pattern != null)
        {
            _patterns.Remove(pattern);
            _logger?.LogDebug("Removed filter pattern: {PatternName}", name);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Enables or disables a pattern
    /// </summary>
    public bool SetPatternEnabled(string name, bool enabled)
    {
        var pattern = _patterns.FirstOrDefault(p => p.Name == name);
        if (pattern != null)
        {
            pattern.IsEnabled = enabled;
            _logger?.LogDebug("Pattern {PatternName} enabled: {Enabled}", name, enabled);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Filters sensitive data from text
    /// </summary>
    public string Filter(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = text;
        var matchCount = 0;

        foreach (var pattern in _patterns.Where(p => p.IsEnabled))
        {
            var beforeCount = CountMatches(result);
            result = pattern.Filter(result, _defaultStrategy);
            var afterCount = CountMatches(result);

            if (beforeCount > afterCount)
            {
                matchCount += (beforeCount - afterCount);
            }
        }

        if (matchCount > 0)
        {
            _logger?.LogDebug("Filtered {MatchCount} sensitive data matches", matchCount);
        }

        return result;
    }

    /// <summary>
    /// Filters sensitive data from a single message
    /// </summary>
    public Chat.ChatMessage FilterMessage(Chat.ChatMessage message)
    {
        return new Chat.ChatMessage
        {
            Sender = message.Sender,
            Content = Filter(message.Content),
            Timestamp = message.Timestamp
        };
    }

    /// <summary>
    /// Filters sensitive data from a list of messages
    /// </summary>
    public IEnumerable<Chat.ChatMessage> FilterMessages(IEnumerable<Chat.ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            yield return FilterMessage(message);
        }
    }

    /// <summary>
    /// Checks if text contains any sensitive data
    /// </summary>
    public bool ContainsSensitiveData(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return _patterns.Any(p => p.IsEnabled && p.ContainsSensitiveData(text));
    }

    /// <summary>
    /// Gets statistics about detected sensitive data
    /// </summary>
    public FilterStatistics GetStatistics(string text)
    {
        var stats = new FilterStatistics();

        if (string.IsNullOrEmpty(text))
            return stats;

        foreach (var pattern in _patterns.Where(p => p.IsEnabled))
        {
            if (pattern.ContainsSensitiveData(text))
            {
                stats.DetectionCount++;
                stats.DetectedPatterns.Add(pattern.Name);
            }
        }

        return stats;
    }

    /// <summary>
    /// Filters text with streaming support for large inputs
    /// </summary>
    public IEnumerable<string> FilterStreaming(IEnumerable<string> lines, int batchSize = 100)
    {
        var batch = new StringBuilder();
        var lineCount = 0;

        foreach (var line in lines)
        {
            batch.AppendLine(line);
            lineCount++;

            if (lineCount >= batchSize)
            {
                yield return Filter(batch.ToString());
                batch.Clear();
                lineCount = 0;
            }
        }

        if (batch.Length > 0)
        {
            yield return Filter(batch.ToString());
        }
    }

    private static int CountMatches(string text)
    {
        var count = 0;
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '*' || text[i] == '[')
                count++;
            i++;
        }
        return count;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _patterns.Clear();
        }
    }
}

/// <summary>
/// Statistics about filtered data
/// </summary>
public class FilterStatistics
{
    /// <summary>
    /// Number of patterns that detected sensitive data
    /// </summary>
    public int DetectionCount { get; set; }

    /// <summary>
    /// Names of detected patterns
    /// </summary>
    public List<string> DetectedPatterns { get; set; } = new();

    /// <summary>
    /// Whether any sensitive data was detected
    /// </summary>
    public bool HasSensitiveData => DetectionCount > 0;
}
