using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

/// <summary>
/// Parsed command result
/// </summary>
public class ParsedCommand
{
    /// <summary>
    /// Original text input
    /// </summary>
    public string OriginalText { get; set; }

    /// <summary>
    /// Command name (without leading /)
    /// </summary>
    public string CommandName { get; set; }

    /// <summary>
    /// Positional arguments
    /// </summary>
    public System.Collections.Generic.List<string> Arguments { get; set; }

    /// <summary>
    /// Named flags (--flag value)
    /// </summary>
    public System.Collections.Generic.Dictionary<string, string> Flags { get; set; }

    /// <summary>
    /// Raw arguments text (without command name)
    /// </summary>
    public string? RawArguments { get; set; }

    /// <summary>
    /// Whether the command is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Parse error message if invalid
    /// </summary>
    public string? ParseError { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public ParsedCommand()
    {
        OriginalText = string.Empty;
        CommandName = string.Empty;
        Arguments = new System.Collections.Generic.List<string>();
        Flags = new System.Collections.Generic.Dictionary<string, string>();
        IsValid = true;
    }

    /// <summary>
    /// Gets a string representation
    /// </summary>
    public override string ToString()
    {
        return $"/{CommandName} {string.Join(" ", Arguments)}";
    }
}

/// <summary>
/// Slash command parser
/// </summary>
public class SlashCommandParser
{
    private static readonly Regex CommandPattern = new Regex(
        @"^/(\w+)(?:\s+(.*))?$",
        RegexOptions.Compiled);

    private static readonly Regex QuotedArgumentPattern = new Regex(
        @"^(?:""([^""]*)""|'([^']*)'|(\S+))",
        RegexOptions.Compiled);

    private static readonly Regex FlagPattern = new Regex(
        @"^--(\w+)(?:\s+(.+))?$",
        RegexOptions.Compiled);

    private readonly ILogger<SlashCommandParser> _logger;

    /// <summary>
    /// Initializes a new instance of SlashCommandParser
    /// </summary>
    public SlashCommandParser(ILogger<SlashCommandParser>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SlashCommandParser>.Instance;
    }

    /// <summary>
    /// Parses a command string into a ParsedCommand
    /// </summary>
    /// <param name="input">Input text (should start with /)</param>
    /// <returns>Parsed command result</returns>
    public ParsedCommand Parse(string input)
    {
        var result = new ParsedCommand
        {
            OriginalText = input
        };

        if (string.IsNullOrWhiteSpace(input))
        {
            result.IsValid = false;
            result.ParseError = "Empty command";
            return result;
        }

        var trimmed = input.TrimStart();

        if (!trimmed.StartsWith("/"))
        {
            result.IsValid = false;
            result.ParseError = "Command must start with /";
            return result;
        }

        var match = CommandPattern.Match(trimmed);
        if (!match.Success)
        {
            result.IsValid = false;
            result.ParseError = "Invalid command format";
            return result;
        }

        result.CommandName = match.Groups[1].Value.ToLowerInvariant();
        result.RawArguments = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;

        if (!string.IsNullOrWhiteSpace(result.RawArguments))
        {
            ParseArguments(result.RawArguments, result);
        }

        _logger.LogDebug($"Parsed command: /{result.CommandName} with {result.Arguments.Count} args");

        return result;
    }

    /// <summary>
    /// Parses arguments string into positional and named arguments
    /// </summary>
    private void ParseArguments(string rawArgs, ParsedCommand command)
    {
        var position = 0;
        var remaining = rawArgs.Trim();

        while (!string.IsNullOrWhiteSpace(remaining))
        {
            var flagMatch = FlagPattern.Match(remaining);
            if (flagMatch.Success)
            {
                var flagName = flagMatch.Groups[1].Value.ToLowerInvariant();
                var flagValue = flagMatch.Groups[2].Success ? flagMatch.Groups[2].Value.Trim() : "true";

                if (command.Flags.ContainsKey(flagName))
                {
                    _logger.LogWarning($"Duplicate flag: {flagName}");
                }

                command.Flags[flagName] = flagValue;
                remaining = remaining.Substring(flagMatch.Length).TrimStart();
            }
            else
            {
                var argMatch = QuotedArgumentPattern.Match(remaining);
                if (argMatch.Success)
                {
                    var arg = argMatch.Groups[1].Success ? argMatch.Groups[1].Value :
                             argMatch.Groups[2].Success ? argMatch.Groups[2].Value :
                             argMatch.Groups[3].Success ? argMatch.Groups[3].Value : string.Empty;

                    command.Arguments.Add(arg);
                    remaining = remaining.Substring(argMatch.Length).TrimStart();
                }
                else
                {
                    command.IsValid = false;
                    command.ParseError = $"Invalid argument at position {position}: {remaining.Substring(0, Math.Min(20, remaining.Length))}";
                    return;
                }
            }

            position++;
        }
    }

    /// <summary>
    /// Checks if input looks like a slash command
    /// </summary>
    public bool IsCommand(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.TrimStart().StartsWith("/");
    }

    /// <summary>
    /// Gets suggestions for command completion
    /// </summary>
    public System.Collections.Generic.List<string> GetSuggestions(
        string partial,
        System.Collections.Generic.List<CommandMetadata> availableCommands)
    {
        var suggestions = new System.Collections.Generic.List<string>();
        var trimmed = partial.TrimStart();

        if (trimmed.StartsWith("/"))
        {
            var partialName = trimmed.Substring(1).ToLowerInvariant();

            foreach (var command in availableCommands)
            {
                if (command.IsAvailable &&
                    (command.Name.StartsWith(partialName) ||
                     command.Aliases.Any(a => a.StartsWith(partialName))))
                {
                    suggestions.Add($"/{command.Name}");
                }
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Validates that arguments match command requirements
    /// </summary>
    public bool ValidateArguments(ParsedCommand command, CommandMetadata metadata)
    {
        var argCount = command.Arguments.Count;

        if (argCount < metadata.MinArguments)
        {
            return false;
        }

        if (metadata.MaxArguments >= 0 && argCount > metadata.MaxArguments)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Formats arguments for display
    /// </summary>
    public string FormatArguments(System.Collections.Generic.List<string> arguments)
    {
        if (arguments.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var arg in arguments)
        {
            if (sb.Length > 0)
            {
                sb.Append(" ");
            }

            if (arg.Contains(" ") || arg.Contains("\""))
            {
                sb.Append($"\"{arg.Replace("\"", "\\\"")}\"");
            }
            else
            {
                sb.Append(arg);
            }
        }

        return sb.ToString();
    }
}
