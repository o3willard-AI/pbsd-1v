using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Security;

/// <summary>
/// Strategy for redacting sensitive data
/// </summary>
public enum RedactionStrategy
{
    /// <summary>
    /// Replace with asterisks (e.g., **** or ****abcd)
    /// </summary>
    Mask,

    /// <summary>
    /// Remove the matched text entirely
    /// </summary>
    Remove,

    /// <summary>
    /// Replace with hash value
    /// </summary>
    Hash,

    /// <summary>
    /// Replace with placeholder text (e.g., [REDACTED])
    /// </summary>
    Placeholder
}

/// <summary>
/// Interface for filter patterns
/// </summary>
public interface IFilterPattern
{
    /// <summary>
    /// Name of the pattern
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what the pattern detects
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Whether the pattern is currently enabled
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Filters sensitive data from text
    /// </summary>
    string Filter(string text, RedactionStrategy strategy);

    /// <summary>
    /// Checks if text contains sensitive data
    /// </summary>
    bool ContainsSensitiveData(string text);
}

/// <summary>
/// Regex-based filter pattern
/// </summary>
public class RegexPattern : IFilterPattern
{
    private readonly Regex _regex;
    private readonly int _preserveLength;
    private readonly string _placeholder;

    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsEnabled { get; set; } = true;

    public RegexPattern(string name, string pattern, int preserveLength = 4, string? placeholder = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = $"Regex pattern for {name}";
        _preserveLength = preserveLength;
        _placeholder = placeholder ?? "[REDACTED]";

        _regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public string Filter(string text, RedactionStrategy strategy)
    {
        if (string.IsNullOrEmpty(text) || !IsEnabled)
            return text;

        return _regex.Replace(text, match => GetReplacement(match, strategy));
    }

    public bool ContainsSensitiveData(string text)
    {
        return !string.IsNullOrEmpty(text) && IsEnabled && _regex.IsMatch(text);
    }

    private string GetReplacement(Match match, RedactionStrategy strategy)
    {
        return strategy switch
        {
            RedactionStrategy.Mask => MaskMatch(match),
            RedactionStrategy.Remove => string.Empty,
            RedactionStrategy.Hash => HashValue(match.Value),
            RedactionStrategy.Placeholder => _placeholder,
            _ => match.Value
        };
    }

    private string MaskMatch(Match match)
    {
        var value = match.Value;
        if (value.Length <= _preserveLength)
            return new string('*', value.Length);

        var visible = value.Substring(0, _preserveLength);
        var masked = new string('*', value.Length - _preserveLength);
        return visible + masked;
    }

    private static string HashValue(string value)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        var hashString = Convert.ToHexString(hash)[..16].ToLowerInvariant();
        return $"#hash:{hashString}";
    }
}

/// <summary>
/// Composite pattern that combines multiple patterns
/// </summary>
public class CompositePattern : IFilterPattern
{
    private readonly List<IFilterPattern> _patterns;

    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsEnabled { get; set; } = true;

    public CompositePattern(string name, string description, IEnumerable<IFilterPattern>? patterns = null)
    {
        Name = name;
        Description = description;
        _patterns = patterns?.ToList() ?? new List<IFilterPattern>();
    }

    public string Filter(string text, RedactionStrategy strategy)
    {
        if (string.IsNullOrEmpty(text) || !IsEnabled)
            return text;

        foreach (var pattern in _patterns.Where(p => p.IsEnabled))
        {
            text = pattern.Filter(text, strategy);
        }

        return text;
    }

    public bool ContainsSensitiveData(string text)
    {
        return !string.IsNullOrEmpty(text) && IsEnabled &&
               _patterns.Any(p => p.IsEnabled && p.ContainsSensitiveData(text));
    }

    public void AddPattern(IFilterPattern pattern)
    {
        _patterns.Add(pattern);
    }

    public IReadOnlyList<IFilterPattern> GetPatterns() => _patterns.AsReadOnly();
}
