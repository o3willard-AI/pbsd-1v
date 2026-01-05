using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class FilterCommandHandler : CommandHandlerBase
{
    private readonly List<FilterEntry> _filters;

    public FilterCommandHandler(ILogger<FilterCommandHandler>? logger = null)
        : base(logger)
    {
        _filters = new List<FilterEntry>();
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "filter",
        Description = "Manages context filters for sensitive data",
        Syntax = "/filter [add|remove|list|clear] <pattern> [--regex]",
        Examples =
        [
            "/filter list",
            "/filter add password:",
            "/filter add api_key_",
            "/filter remove password:",
            "/filter --regex \"(?i)secret\"",
            "/filter clear"
        ],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 2,
        Aliases = ["mask", "redact"]
    }

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var subCommand = command.Arguments.Count > 0 ? command.Arguments[0].ToLowerInvariant() : "list";
        var pattern = command.Arguments.Count > 1 ? command.Arguments[1] : null;
        var isRegex = command.Flags.ContainsKey("regex");

        return subCommand switch
        {
            "add" => AddFilter(pattern, isRegex),
            "remove" or "delete" or "del" => RemoveFilter(pattern),
            "list" or "show" => ListFilters(),
            "clear" or "reset" => ClearFilters(),
            _ => InvalidArguments("/filter [add|remove|list|clear] <pattern>")
        };
    }

    private CommandResult AddFilter(string? pattern, bool isRegex)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return InvalidArguments("/filter add <pattern>");
        }

        try
        {
            if (isRegex)
            {
                new Regex(pattern);
            }

            var existing = _filters.FirstOrDefault(f => f.Pattern == pattern);
            if (existing != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("## Filter Already Exists");
                sb.AppendLine();
                sb.AppendLine($"Pattern '{pattern}' is already in the filter list.");
                return Success(sb.ToString());
            }

            _filters.Add(new FilterEntry
            {
                Pattern = pattern,
                IsRegex = isRegex,
                CreatedAt = DateTime.Now
            });

            _logger.LogInformation("Added filter: {Pattern} (regex: {IsRegex})", pattern, isRegex);

            var sb = new StringBuilder();
            sb.AppendLine("## Filter Added");
            sb.AppendLine();
            sb.AppendLine($"**Pattern:** `{pattern}`");
            sb.AppendLine($"**Type:** {(isRegex ? "Regex" : "Text")}");
            sb.AppendLine($"**Total Filters:** {_filters.Count}");
            sb.AppendLine();

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add filter");
            return Error($"Invalid pattern: {ex.Message}");
        }
    }

    private CommandResult RemoveFilter(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return InvalidArguments("/filter remove <pattern>");
        }

        var filter = _filters.FirstOrDefault(f => f.Pattern == pattern);
        if (filter == null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Filter Not Found");
            sb.AppendLine();
            sb.AppendLine($"Pattern '{pattern}' is not in the filter list.");
            sb.AppendLine();
            sb.AppendLine("Current filters:");
            foreach (var f in _filters)
            {
                sb.AppendLine($"- {f.Pattern}");
            }
            return Error(sb.ToString());
        }

        _filters.Remove(filter);
        _logger.LogInformation("Removed filter: {Pattern}", pattern);

        var sb2 = new StringBuilder();
        sb2.AppendLine("## Filter Removed");
        sb2.AppendLine();
        sb2.AppendLine($"**Pattern:** `{pattern}`");
        sb2.AppendLine($"**Remaining Filters:** {_filters.Count}");

        return Success(sb2.ToString());
    }

    private CommandResult ListFilters()
    {
        if (_filters.Count == 0)
        {
            return Success("## No Filters\n\nNo filters are currently active. Use `/filter add <pattern>` to add one.");
        }

        var sb = new StringBuilder();
        sb.AppendLine("## Active Filters");
        sb.AppendLine();
        sb.AppendLine($"Total: {_filters.Count} filter(s)");
        sb.AppendLine();

        for (int i = 0; i < _filters.Count; i++)
        {
            var filter = _filters[i];
            sb.AppendLine($"{i + 1}. `{filter.Pattern}`");
            sb.AppendLine($"   Type: {(filter.IsRegex ? "Regex" : "Text")}");
            sb.AppendLine($"   Added: {filter.CreatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
        }

        sb.AppendLine("**Usage:**");
        sb.AppendLine("- `/filter add <pattern>` - Add new filter");
        sb.AppendLine("- `/filter remove <pattern>` - Remove filter");
        sb.AppendLine("- `/filter clear` - Remove all filters");
        sb.AppendLine("- `/filter --regex <pattern>` - Use regex pattern");

        return Success(sb.ToString());
    }

    private CommandResult ClearFilters()
    {
        if (_filters.Count == 0)
        {
            return Success("## No Filters to Clear\n\nThere are no filters to remove.");
        }

        var count = _filters.Count;
        _filters.Clear();
        _logger.LogInformation("Cleared {Count} filters", count);

        var sb = new StringBuilder();
        sb.AppendLine("## Filters Cleared");
        sb.AppendLine();
        sb.AppendLine($"Removed {count} filter(s).");

        return Success(sb.ToString());
    }

    public bool ApplyFilters(ref string text)
    {
        foreach (var filter in _filters)
        {
            if (filter.IsRegex)
            {
                try
                {
                    text = Regex.Replace(text, filter.Pattern, "[REDACTED]");
                }
                catch (RegexMatchTimeoutException)
                {
                    _logger.LogWarning("Regex filter timed out: {Pattern}", filter.Pattern);
                }
            }
            else
            {
                text = text.Replace(filter.Pattern, "[REDACTED]");
            }
        }

        return _filters.Count > 0;
    }

    public IReadOnlyList<FilterEntry> GetFilters() => _filters.AsReadOnly();

    private class FilterEntry
    {
        public string Pattern { get; set; } = string.Empty;
        public bool IsRegex { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
