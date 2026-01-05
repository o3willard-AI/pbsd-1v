using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Help;

/// <summary>
/// Service for command reference documentation
/// </summary>
public class CommandReferenceService
{
    private readonly Dictionary<string, CommandReference> _commands;
    private readonly ILogger<CommandReferenceService>? _logger;

    public CommandReferenceService(ILogger<CommandReferenceService>? logger = null)
    {
        _logger = logger;
        _commands = new Dictionary<string, CommandReference>(StringComparer.OrdinalIgnoreCase);
        InitializeReferences();
        _logger?.LogInformation("CommandReferenceService initialized with {Count} commands", _commands.Count);
    }

    /// <summary>
    /// Gets reference for a command
    /// </summary>
    public CommandReference? GetCommand(string name)
    {
        var normalized = name.TrimStart('/').ToLowerInvariant();
        return _commands.TryGetValue(normalized, out var cmd) ? cmd : null;
    }

    /// <summary>
    /// Gets all commands in a category
    /// </summary>
    public IEnumerable<CommandReference> GetByCategory(string category)
    {
        return _commands.Values
            .Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name);
    }

    /// <summary>
    /// Searches commands
    /// </summary>
    public IEnumerable<CommandReference> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<CommandReference>();

        var q = query.ToLowerInvariant();
        return _commands.Values
            .Where(c =>
                c.Name.Contains(q) ||
                c.Description.Contains(q) ||
                c.Category.Contains(q) ||
                c.Aliases.Any(a => a.Contains(q)) ||
                c.DetailedDescription.Contains(q))
            .OrderBy(c => c.Name);
    }

    /// <summary>
    /// Gets all categories
    /// </summary>
    public IEnumerable<string> GetCategories()
    {
        return _commands.Values
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c);
    }

    /// <summary>
    /// Gets command summary for a category
    /// </summary>
    public string GetCategorySummary(string category)
    {
        var commands = GetByCategory(category).ToList();
        if (!commands.Any())
            return $"No commands found in category: {category}";

        var sb = new StringBuilder();
        sb.AppendLine($"## {category}");
        sb.AppendLine();
        sb.AppendLine("| Command | Description |");
        sb.AppendLine("|---------|-------------|");

        foreach (var cmd in commands)
        {
            sb.AppendLine($"| `/{cmd.Name}` | {cmd.Description} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets complete command reference as Markdown
    /// </summary>
    public string GetFullReference()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PairAdmin Command Reference");
        sb.AppendLine();
        sb.AppendLine($"*Generated: {DateTime.UtcNow:yyyy-MM-dd}*");
        sb.AppendLine();

        foreach (var category in GetCategories())
        {
            sb.AppendLine(GetCategorySummary(category));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets quick reference card
    /// </summary>
    public string GetQuickReference()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Quick Reference");
        sb.AppendLine();
        sb.AppendLine("### Core Commands");
        sb.AppendLine("```");
        sb.AppendLine("/help [cmd]  - Get help");
        sb.AppendLine("/clear       - Clear chat");
        sb.AppendLine("/context     - Manage context");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Configuration");
        sb.AppendLine("```");
        sb.AppendLine("/model [name]  - Set LLM model");
        sb.AppendLine("/theme [opt]   - Set theme");
        sb.AppendLine("/mode [opt]    - Set autonomy");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Utility");
        sb.AppendLine("```");
        sb.AppendLine("/save-log      - Export log");
        sb.AppendLine("/export        - Export chat");
        sb.AppendLine("/filter        - Manage filters");
        sb.AppendLine("/status        - Show status");
        sb.AppendLine("```");

        return sb.ToString();
    }

    /// <summary>
    /// Gets all commands as a list
    /// </summary>
    public IReadOnlyDictionary<string, CommandReference> GetAllCommands() =>
        new Dictionary<string, CommandReference>(_commands);

    /// <summary>
    /// Gets command count
    /// </summary>
    public int GetCommandCount() => _commands.Count;

    private void InitializeReferences()
    {
        InitializeCoreCommands();
        InitializeConfigCommands();
        InitializeUtilityCommands();
        InitializeSecurityCommands();
    }

    private void InitializeCoreCommands()
    {
        _commands["help"] = new CommandReference
        {
            Name = "help",
            Description = "Displays help information for commands",
            DetailedDescription = "The help command provides access to PairAdmin's comprehensive documentation. " +
                "You can view all commands, get detailed help for specific commands, or search for topics.",
            Category = "Core",
            Syntax = "/help [command|category|search <query>]",
            Examples = new List<string> { "/help", "/help context", "/help core-commands", "/help search model" },
            Arguments = new List<CommandArgument>
            {
                new CommandArgument { Name = "command|category|search", Required = false, Description = "Specific command, category name, or 'search' keyword" }
            },
            Aliases = new List<string> { "h", "?" },
            RelatedCommands = new List<string> { "status", "context" },
            Tips = new List<string>
            {
                "Use /help search <term> to find topics",
                "All commands support /h as a shortcut"
            }
        };

        _commands["clear"] = new CommandReference
        {
            Name = "clear",
            Description = "Clears chat history and resets the conversation",
            DetailedDescription = "The clear command removes messages and/or context from the current session. " +
                "This is useful when starting a new topic or when the conversation has become too long.",
            Category = "Core",
            Syntax = "/clear [all|messages|context]",
            Examples = new List<string> { "/clear", "/clear messages", "/clear context" },
            Arguments = new List<CommandArgument>
            {
                new CommandArgument { Name = "scope", Required = false, Description = "Scope: 'all', 'messages', or 'context'", DefaultValue = "all" }
            },
            Aliases = new List<string> { "cls", "reset" },
            RelatedCommands = new List<string> { "save-log", "context" },
            Tips = new List<string>
            {
                "Use /clear messages to keep context but start fresh chat",
                "Use /clear context to clear history but keep current discussion"
            }
        };

        _commands["context"] = new CommandReference
        {
            Name = "context",
            Description = "Manages the context window size",
            DetailedDescription = "The context command controls how much conversation history is sent to the AI. " +
                "A larger context provides more history but costs more tokens.",
            Category = "Core",
            Syntax = "/context [show|<lines>|--percentage <0-100>|--auto]",
            Examples = new List<string> { "/context", "/context 100", "/context 50%", "/context --auto" },
            Arguments = new List<CommandArgument>
            {
                new CommandArgument { Name = "lines|percentage", Required = false, Description = "Number of lines or percentage with % suffix" }
            },
            Flags = new List<CommandFlag>
            {
                new CommandFlag { Name = "percentage", Description = "Set context as percentage of maximum", RequiresValue = true },
                new CommandFlag { Name = "auto", Description = "Use model default context size", RequiresValue = false }
            },
            Aliases = new List<string> { "ctx", "window" },
            RelatedCommands = new List<string> { "clear" },
            Tips = new List<string>
            {
                "Auto mode is recommended for most users",
                "Smaller context = faster responses",
                "Larger context = more history but slower"
            }
        };
    }

    private void InitializeConfigCommands()
    {
        _commands["model"] = new CommandReference
        {
            Name = "model",
            Description = "Manages the LLM model and provider",
            DetailedDescription = "The model command allows you to view and change the LLM model used for AI responses.",
            Category = "Configuration",
            Syntax = "/model [show|<model>|--list|--provider <provider-id>]",
            Examples = new List<string> { "/model", "/model gpt-4", "/model --list", "/model --provider openai" },
            Arguments = new List<CommandArgument>
            {
                new CommandArgument { Name = "model", Required = false, Description = "Model name to use" }
            },
            Flags = new List<CommandFlag>
            {
                new CommandFlag { Name = "list", Description = "List all available models", RequiresValue = false },
                new CommandFlag { Name = "provider", Description = "Switch to a different provider", RequiresValue = true }
            },
            Aliases = new List<string> { "m" },
            RelatedCommands = new List<string> { "status" },
            Tips = new List<string> { "Use /model --list to see all available models" }
        };

        _commands["theme"] = new CommandReference
        {
            Name = "theme",
            Description = "Manages the application theme and appearance",
            DetailedDescription = "The theme command allows you to customize the visual appearance of PairAdmin.",
            Category = "Configuration",
            Syntax = "/theme [show|light|dark|system|<accent-color>|--font-size <size>]",
            Examples = new List<string> { "/theme", "/theme dark", "/theme #3498DB", "/theme --font-size 16" },
            Aliases = new List<string> { "t", "color", "style" },
            RelatedCommands = new List<string> { "config" },
            Tips = new List<string>
            {
                "Use system mode to match your OS theme",
                "Accent colors use #RRGGBB format"
            }
        };

        _commands["mode"] = new CommandReference
        {
            Name = "mode",
            Description = "Manages the AI autonomy mode (stub)",
            DetailedDescription = "The mode command controls how the AI interacts with your terminal. " +
                "This feature requires Windows integration.",
            Category = "Configuration",
            Syntax = "/mode [show|manual|auto|confirm]",
            Examples = new List<string> { "/mode", "/mode manual", "/mode auto" },
            RelatedCommands = new List<string> { "status" },
            Tips = new List<string> { "Requires Phase 5 implementation" },
            RequiredPrivilege = Security.PrivilegeLevel.Elevated
        };
    }

    private void InitializeUtilityCommands()
    {
        _commands["save-log"] = new CommandReference
        {
            Name = "save-log",
            Description = "Exports the session log to a file",
            DetailedDescription = "The save-log command exports your current session log to a file for later review.",
            Category = "Utility",
            Syntax = "/save-log [path] [--format text|json]",
            Examples = new List<string> { "/save-log", "/save-log session.log", "/save-log --format json out.json" },
            Aliases = new List<string> { "log", "save" },
            RelatedCommands = new List<string> { "export", "clear" },
            Tips = new List<string>
            {
                "Logs are saved to Documents/PairAdmin/Logs by default",
                "JSON format is useful for programmatic processing"
            }
        };

        _commands["export"] = new CommandReference
        {
            Name = "export",
            Description = "Exports chat history to clipboard or file",
            DetailedDescription = "The export command allows you to copy or save your chat history.",
            Category = "Utility",
            Syntax = "/export [--format text|json] [--copy]",
            Examples = new List<string> { "/export", "/export --format json", "/export --copy" },
            Aliases = new List<string> { "save-chat", "download" },
            RelatedCommands = new List<string> { "save-log" },
            Tips = new List<string> { "Use --copy to quickly copy to clipboard" }
        };

        _commands["filter"] = new CommandReference
        {
            Name = "filter",
            Description = "Manages context filters for sensitive data",
            DetailedDescription = "The filter command allows you to add patterns that will be automatically " +
                "redacted from terminal output before being sent to the AI.",
            Category = "Utility",
            Syntax = "/filter [add|remove|list|clear] <pattern> [--regex]",
            Examples = new List<string> { "/filter list", "/filter add password:", "/filter --regex \"(?i)secret\"" },
            Aliases = new List<string> { "mask", "redact" },
            RelatedCommands = new List<string> { "status" },
            Tips = new List<string>
            {
                "Use regex patterns for complex matching",
                "Filters apply to all terminal output"
            }
        };

        _commands["status"] = new CommandReference
        {
            Name = "status",
            Description = "Shows system status and diagnostics",
            DetailedDescription = "The status command displays current system information including " +
                "memory usage, message count, and provider status.",
            Category = "Utility",
            Syntax = "/status",
            Examples = new List<string> { "/status" },
            Aliases = new List<string> { "stats", "info" },
            RelatedCommands = new List<string> { "config", "model" },
            Tips = new List<string> { "Check this to diagnose issues" }
        };

        _commands["config"] = new CommandReference
        {
            Name = "config",
            Description = "Manages application configuration",
            DetailedDescription = "The config command shows configuration file location and allows reloading settings.",
            Category = "Utility",
            Syntax = "/config [show|open|reload]",
            Examples = new List<string> { "/config", "/config show", "/config open", "/config reload" },
            Aliases = new List<string> { "settings", "conf" },
            RelatedCommands = new List<string> { "theme", "model" },
            Tips = new List<string> { "Use /config open to edit settings file" }
        };

        _commands["quit"] = new CommandReference
        {
            Name = "quit",
            Description = "Closes the application",
            DetailedDescription = "The quit command closes PairAdmin. Unsaved data may be lost.",
            Category = "Utility",
            Syntax = "/quit [--force]",
            Examples = new List<string> { "/quit", "/quit --force" },
            Aliases = new List<string> { "exit", "close", "q" },
            Tips = new List<string> { "Use --force to skip confirmation" }
        };
    }

    private void InitializeSecurityCommands()
    {
        _commands["audit"] = new CommandReference
        {
            Name = "audit",
            Description = "View audit log (via status)",
            DetailedDescription = "Security audit logging is automatic. Use /status to view security-related information.",
            Category = "Security",
            Syntax = "(automatic)",
            Examples = new List<string> { "/status" },
            RelatedCommands = new List<string> { "filter", "status" },
            Tips = new List<string> { "Audit logs are automatically maintained" }
        };
    }
}
