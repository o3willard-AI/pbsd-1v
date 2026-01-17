using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Context;

/// <summary>
/// Shell prompt type
/// </summary>
public enum PromptType
{
    Bash,          // [user@host:~]$
    Zsh,           // [user@host ~]%
    Fish,           // user@host /path/to/dir
    PowerShell,    // PS C:\Users\user>
    Custom          // User-defined pattern
}

/// <summary>
/// Parsed prompt information
/// </summary>
public class ParsedPrompt
{
    /// <summary>
    /// Original text from terminal
    /// </summary>
    public string OriginalText { get; set; }

    /// <summary>
    /// Detected prompt type
    /// </summary>
    public PromptType Type { get; set; }

    /// <summary>
    /// Username from prompt
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Hostname from prompt
    /// </summary>
    public string? Hostname { get; set; }

    /// <summary>
    /// Current directory from prompt
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>
    /// Whether prompt was successfully parsed
    /// </summary>
    public bool IsDetected { get; set; }

    /// <summary>
    /// Gets the expanded directory path
    /// </summary>
    /// <param name="homeDir">Home directory for expansion</param>
    /// <returns>Expanded directory path</returns>
    public string GetExpandedDirectory(string homeDir)
    {
        if (string.IsNullOrWhiteSpace(Directory))
        {
            return string.Empty;
        }

        var dir = Directory;
        if (dir.StartsWith("~"))
        {
            dir = Path.Combine(homeDir, dir.Substring(1));
        }

        return dir;
    }

    /// <summary>
    /// Creates a copy of this prompt
    /// </summary>
    public ParsedPrompt Clone()
    {
        return new ParsedPrompt
        {
            OriginalText = OriginalText,
            Type = Type,
            Username = Username,
            Hostname = Hostname,
            Directory = Directory,
            IsDetected = IsDetected
        };
    }
}

/// <summary>
/// Terminal prompt parser for extracting working directory
/// </summary>
public class TerminalPromptParser
{
    private readonly ILogger<TerminalPromptParser> _logger;

    /// <summary>
    /// Bash prompt pattern: [user@host:~]$
    /// </summary>
    private static readonly Regex BashPromptPattern = new Regex(
        @"^([^@]+)@([^:]+):([^ ]*)\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Zsh prompt pattern: [user@host ~]%
    /// </summary>
    private static readonly Regex ZshPromptPattern = new Regex(
        @"^([^@]+)@([^ ]+)\s+([^%]+)%\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Fish prompt pattern: user@host /path/to/dir
    /// </summary>
    private static readonly Regex FishPromptPattern = new Regex(
        @"^([^@]+)@([^ ]+)\s+([^\s]+)(?:\s+)?$",
        RegexOptions.Compiled);

    /// <summary>
    /// PowerShell prompt pattern: PS C:\Users\user>
    /// </summary>
    private static readonly Regex PowerShellPromptPattern = new Regex(
        @"^PS\s+[^>]+>\s+(.+)$",
        RegexOptions.Compiled);

    /// <summary>
    /// User-defined custom patterns
    /// </summary>
    private List<Regex> _customPatterns;

    /// <summary>
    /// Home directory
    /// </summary>
    private string _homeDirectory;

    /// <summary>
    /// Initializes a new instance of TerminalPromptParser
    /// </summary>
    public TerminalPromptParser(ILogger<TerminalPromptParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _customPatterns = new List<Regex>();
        _homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        _logger.LogInformation($"TerminalPromptParser initialized. Home dir: {_homeDirectory}");
    }

    /// <summary>
    /// Parses terminal output to extract working directory
    /// </summary>
    /// <param name="terminalOutput">Terminal output line</param>
    /// <returns>Parsed prompt information</returns>
    public ParsedPrompt ParsePrompt(string terminalOutput)
    {
        if (string.IsNullOrWhiteSpace(terminalOutput))
        {
            return new ParsedPrompt
            {
                OriginalText = terminalOutput,
                Type = PromptType.Bash,
                IsDetected = false
            };
        }

        var parsed = TryParseBash(terminalOutput);
        if (parsed.IsDetected)
        {
            return parsed;
        }

        parsed = TryParseZsh(terminalOutput);
        if (parsed.IsDetected)
        {
            return parsed;
        }

        parsed = TryParseFish(terminalOutput);
        if (parsed.IsDetected)
        {
            return parsed;
        }

        parsed = TryParsePowerShell(terminalOutput);
        if (parsed.IsDetected)
        {
            return parsed;
        }

        foreach (var pattern in _customPatterns)
        {
            parsed = TryParseCustom(terminalOutput, pattern);
            if (parsed.IsDetected)
            {
                return parsed;
            }
        }

        return new ParsedPrompt
        {
            OriginalText = terminalOutput,
            Type = PromptType.Bash,
            IsDetected = false
        };
    }

    /// <summary>
    /// Tries to parse bash prompt
    /// </summary>
    private ParsedPrompt TryParseBash(string text)
    {
        var match = BashPromptPattern.Match(text);
        if (match.Success)
        {
            return new ParsedPrompt
            {
                OriginalText = text,
                Type = PromptType.Bash,
                Username = match.Groups[1].Value,
                Hostname = match.Groups[2].Value,
                Directory = match.Groups[3].Value,
                IsDetected = true
            };
        }

        return new ParsedPrompt { OriginalText = text, Type = PromptType.Bash, IsDetected = false };
    }

    /// <summary>
    /// Tries to parse zsh prompt
    /// </summary>
    private ParsedPrompt TryParseZsh(string text)
    {
        var match = ZshPromptPattern.Match(text);
        if (match.Success)
        {
            return new ParsedPrompt
            {
                OriginalText = text,
                Type = PromptType.Zsh,
                Username = match.Groups[1].Value,
                Hostname = match.Groups[2].Value,
                Directory = match.Groups[3].Value,
                IsDetected = true
            };
        }

        return new ParsedPrompt { OriginalText = text, Type = PromptType.Zsh, IsDetected = false };
    }

    /// <summary>
    /// Tries to parse fish prompt
    /// </summary>
    private ParsedPrompt TryParseFish(string text)
    {
        var match = FishPromptPattern.Match(text);
        if (match.Success)
        {
            return new ParsedPrompt
            {
                OriginalText = text,
                Type = PromptType.Fish,
                Username = match.Groups[1].Value,
                Hostname = match.Groups[2].Value,
                Directory = match.Groups[3].Value,
                IsDetected = true
            };
        }

        return new ParsedPrompt { OriginalText = text, Type = PromptType.Fish, IsDetected = false };
    }

    /// <summary>
    /// Tries to parse PowerShell prompt
    /// </summary>
    private ParsedPrompt TryParsePowerShell(string text)
    {
        var match = PowerShellPromptPattern.Match(text);
        if (match.Success)
        {
            return new ParsedPrompt
            {
                OriginalText = text,
                Type = PromptType.PowerShell,
                Username = null,
                Hostname = null,
                Directory = match.Groups[1].Value,
                IsDetected = true
            };
        }

        return new ParsedPrompt { OriginalText = text, Type = PromptType.PowerShell, IsDetected = false };
    }

    /// <summary>
    /// Tries to parse with custom pattern
    /// </summary>
    private ParsedPrompt TryParseCustom(string text, Regex pattern)
    {
        var match = pattern.Match(text);
        if (match.Success && match.Groups.Count > 3)
        {
            return new ParsedPrompt
            {
                OriginalText = text,
                Type = PromptType.Custom,
                Username = match.Groups[1].Value,
                Hostname = match.Groups[2].Value,
                Directory = match.Groups[3].Value,
                IsDetected = true
            };
        }

        return new ParsedPrompt { OriginalText = text, Type = PromptType.Custom, IsDetected = false };
    }

    /// <summary>
    /// Adds a custom prompt pattern
    /// </summary>
    /// <param name="pattern">Regex pattern</param>
    public void AddCustomPattern(Regex pattern)
    {
        _customPatterns.Add(pattern);
        _logger.LogInformation($"Added custom prompt pattern");
    }

    /// <summary>
    /// Clears custom patterns
    /// </summary>
    public void ClearCustomPatterns()
    {
        _customPatterns.Clear();
        _logger.LogInformation("Cleared custom prompt patterns");
    }

    /// <summary>
    /// Gets the home directory
    /// </summary>
    /// <returns>Home directory path</returns>
    public string GetHomeDirectory()
    {
        return _homeDirectory;
    }

    /// <summary>
    /// Sets the home directory
    /// </summary>
    /// <param name="homeDir">Home directory path</param>
    public void SetHomeDirectory(string homeDir)
    {
        _homeDirectory = homeDir;
        _logger.LogInformation($"Home directory set to {homeDir}");
    }

    /// <summary>
    /// Gets detected prompt type
    /// </summary>
    /// <param name="text">Terminal text to analyze</param>
    /// <returns>Detected prompt type</returns>
    public PromptType DetectPromptType(string text)
    {
        var parsed = ParsePrompt(text);
        return parsed.Type;
    }

    /// <summary>
    /// Gets all supported prompt types
    /// </summary>
    /// <returns>List of supported prompt types</returns>
    public List<PromptType> GetSupportedPromptTypes()
    {
        return new List<PromptType>
        {
            PromptType.Bash,
            PromptType.Zsh,
            PromptType.Fish,
            PromptType.PowerShell
        };
    }
}
